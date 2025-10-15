using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace PdfDownloader.App.Downloads;

internal sealed class DownloadManager : IDisposable
{
    private readonly DirectoryInfo _outputDirectory;
    private readonly HttpClient _httpClient;
    private readonly int _maxConcurrency;
    private readonly bool _skipExisting;
    private readonly bool _overwriteDownloads;
    private readonly bool _detectChanges;
    private readonly bool _keepOldOnChange;

    // NEW — timeouts
    private readonly TimeSpan _downloadTimeout;
    private readonly TimeSpan _idleTimeout;
    private readonly bool _noTimeout;
    private readonly TimeSpan _connectTimeout;

    private readonly Action<string> _log;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _startTimes = new();
    private readonly ConcurrentDictionary<string, string> _states = new();

    private readonly ConcurrentQueue<int> _slotPool;
    private readonly ConcurrentDictionary<string, int> _slotOfId = new();
    private readonly ConcurrentDictionary<int, long> _slotTotalMs = new();
    private readonly ConcurrentDictionary<int, int> _slotCounts = new();

    // Prod-ctor
    public DownloadManager(
        DirectoryInfo outputDirectory,
        int maxConcurrency,
        bool skipExisting,
        bool overwriteDownloads,
        bool detectChanges,
        bool keepOldOnChange,
        TimeSpan downloadTimeout,   // NEW
        TimeSpan idleTimeout,       // NEW
        bool noTimeout,             // NEW
        TimeSpan connectTimeout,    // NEW
        Action<string>? logger = null)
    {
        _outputDirectory = outputDirectory;

        _downloadTimeout = downloadTimeout;
        _idleTimeout = idleTimeout;
        _noTimeout = noTimeout;
        _connectTimeout = connectTimeout;

        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 256,
            EnableMultipleHttp2Connections = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            ConnectTimeout = _connectTimeout
        };

        _httpClient = new HttpClient(handler)
        {
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
            Timeout = _noTimeout
                ? Timeout.InfiniteTimeSpan
                : (_downloadTimeout > TimeSpan.Zero ? _downloadTimeout : Timeout.InfiniteTimeSpan)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PdfDownloader", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/pdf");

        _maxConcurrency = Math.Max(1, maxConcurrency);
        _skipExisting = skipExisting;
        _overwriteDownloads = overwriteDownloads;
        _detectChanges = detectChanges;
        _keepOldOnChange = keepOldOnChange;

        _log = logger ?? Console.WriteLine;

        _slotPool = new ConcurrentQueue<int>(Enumerable.Range(1, _maxConcurrency));
        foreach (var s in Enumerable.Range(1, _maxConcurrency))
        {
            _slotTotalMs[s] = 0;
            _slotCounts[s] = 0;
        }
    }

    // Test-ctor (tillader injektion af HttpMessageHandler)
    internal DownloadManager(
        DirectoryInfo outputDirectory,
        int maxConcurrency,
        bool skipExisting,
        bool overwriteDownloads,
        bool detectChanges,
        bool keepOldOnChange,
        TimeSpan downloadTimeout,
        TimeSpan idleTimeout,
        bool noTimeout,
        TimeSpan connectTimeout,
        HttpMessageHandler httpHandler,
        Action<string>? logger = null)
    {
        _outputDirectory = outputDirectory;

        _downloadTimeout = downloadTimeout;
        _idleTimeout = idleTimeout;
        _noTimeout = noTimeout;
        _connectTimeout = connectTimeout;

        _httpClient = new HttpClient(httpHandler)
        {
            Timeout = _noTimeout
                ? Timeout.InfiniteTimeSpan
                : (_downloadTimeout > TimeSpan.Zero ? _downloadTimeout : Timeout.InfiniteTimeSpan)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PdfDownloader", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/pdf");

        _maxConcurrency = Math.Max(1, maxConcurrency);
        _skipExisting = skipExisting;
        _overwriteDownloads = overwriteDownloads;
        _detectChanges = detectChanges;
        _keepOldOnChange = keepOldOnChange;

        _log = logger ?? Console.WriteLine;

        _slotPool = new ConcurrentQueue<int>(Enumerable.Range(1, _maxConcurrency));
        foreach (var s in Enumerable.Range(1, _maxConcurrency))
        {
            _slotTotalMs[s] = 0;
            _slotCounts[s] = 0;
        }
    }

    public async Task<IReadOnlyList<DownloadResult>> DownloadAsync(IEnumerable<DownloadRequest> requests, CancellationToken ct)
    {
        Directory.CreateDirectory(_outputDirectory.FullName);

        var results = new ConcurrentBag<DownloadResult>();
        using var throttler = new SemaphoreSlim(_maxConcurrency);

        var tasks = requests.Select(async req =>
        {
            await throttler.WaitAsync(ct).ConfigureAwait(false);
            bool sawTimeout = false; // NEW
            string? lastReason = null;
            var slot = RentSlot();

            try
            {
                StartRow(req.Id, "Klargør", slot);

                var file = new FileInfo(Path.Combine(_outputDirectory.FullName, SanitizeFileName(req.Id) + ".pdf"));

                if (file.Exists && !_overwriteDownloads && _skipExisting)
                {
                    var res = new DownloadResult(req.Id, DownloadOutcome.SkippedExisting, "Already exists", null, file);
                    CompleteRow(res, slot);
                    results.Add(res);
                    return;
                }

                foreach (var url in req.Urls)
                {
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    // NEW — per-fil timeout der kan skelne fra global cancellation
                    using var perFileCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    if (!_noTimeout && _downloadTimeout > TimeSpan.Zero)
                        perFileCts.CancelAfter(_downloadTimeout);

                    try
                    {
                        UpdateRow(req.Id, "Henter (headers)...", slot);
                        using var resp = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, perFileCts.Token).ConfigureAwait(false);

                        if (!resp.IsSuccessStatusCode)
                        {
                            lastReason = $"HTTP {(int)resp.StatusCode}";
                            UpdateRow(req.Id, $"{lastReason} – prøver næste", slot);
                            continue;
                        }

                        var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
                        var isPdf = contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase);
                        if (!isPdf)
                        {
                            lastReason = $"Content-Type: {contentType}";
                            UpdateRow(req.Id, $"{lastReason} – skipper", slot);
                            continue;
                        }

                        var tempPath = Path.GetTempFileName();
                        UpdateRow(req.Id, "Downloader indhold...", slot);
                        await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
                        await using (var net = await resp.Content.ReadAsStreamAsync(perFileCts.Token))
                        {
                            if (_noTimeout || _idleTimeout <= TimeSpan.Zero)
                            {
                                await net.CopyToAsync(fs, 64 * 1024, perFileCts.Token).ConfigureAwait(false);
                            }
                            else
                            {
                                await CopyToAsyncWithIdleTimeout(net, fs, 64 * 1024, _idleTimeout, perFileCts.Token).ConfigureAwait(false);
                            }
                        }

                        if (file.Exists && (_overwriteDownloads || _detectChanges))
                        {
                            bool changed = true;
                            if (_detectChanges)
                            {
                                var oldHash = ComputeSHA256(file.FullName);
                                var newHash = ComputeSHA256(tempPath);
                                changed = !oldHash.SequenceEqual(newHash);
                            }

                            if (!changed && !_overwriteDownloads)
                            {
                                File.Delete(tempPath);
                                var noChange = new DownloadResult(req.Id, DownloadOutcome.SkippedExisting, "No change detected", new Uri(url), file);
                                CompleteRow(noChange, slot);
                                results.Add(noChange);
                                return;
                            }

                            if (changed && _keepOldOnChange)
                            {
                                var updated = Path.Combine(file.Directory!.FullName, Path.GetFileNameWithoutExtension(file.Name) + ".updated.pdf");
                                if (File.Exists(updated))
                                {
                                    updated = Path.Combine(file.Directory!.FullName, Path.GetFileNameWithoutExtension(file.Name) + $".updated-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
                                }
                                File.Move(file.FullName, updated, overwrite: false);
                            }
                        }

                        File.Copy(tempPath, file.FullName, overwrite: true);
                        File.Delete(tempPath);

                        var ok = new DownloadResult(req.Id, DownloadOutcome.Downloaded, null, new Uri(url), file);
                        CompleteRow(ok, slot);
                        results.Add(ok);
                        return;
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                    {
                        sawTimeout = true;
                        lastReason = "Timeout";
                        UpdateRow(req.Id, "Timeout – prøver næste", slot);
                        continue;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        lastReason = $"Exception: {ex.GetType().Name}";
                        UpdateRow(req.Id, "Fejl – prøver næste URL", slot);
                        continue;
                    }
                }

                var msg = req.Urls.Count == 0 ? "No URL" : (lastReason ?? "All URLs failed");
                var outcome = req.Urls.Count == 0
                    ? DownloadOutcome.NoUrl
                    : (sawTimeout ? DownloadOutcome.TimedOut : DownloadOutcome.Failed);

                var failed = new DownloadResult(req.Id, outcome, msg, null, null);
                CompleteRow(failed, slot);
                results.Add(failed);
            }
            finally
            {
                _startTimes.TryRemove(req.Id, out _);
                _states.TryRemove(req.Id, out _);
                _slotOfId.TryRemove(req.Id, out _);
                ReturnSlot(slot);
                throttler.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.ToList();
    }

    // ---------- helpers: log + tid + slots ----------
    private void StartRow(string id, string state, int slot)
    {
        _startTimes.TryAdd(id, DateTimeOffset.UtcNow);
        _states[id] = state;
        _slotOfId[id] = slot;
        _log($"[START] {id} - Klargør");
    }

    private void UpdateRow(string id, string state, int slot)
    {
        _states[id] = state;
        _log($"[UPDATE] {id} [{ElapsedFor(id)}] - {state}");
    }

    private void CompleteRow(DownloadResult result, int slot)
    {
        var id = result.Id;
        var elapsed = ElapsedFor(id);

        if (_startTimes.TryGetValue(id, out var t0))
        {
            var ms = (long)(DateTimeOffset.UtcNow - t0).TotalMilliseconds;
            _slotTotalMs.AddOrUpdate(slot, ms, (_, cur) => cur + ms);
            _slotCounts.AddOrUpdate(slot, 1, (_, cur) => cur + 1);
        }

        var (tag, human) = result.Outcome switch
        {
            DownloadOutcome.Downloaded      => ("OK",   "PDF gemt korrekt"),
            DownloadOutcome.SkippedExisting => ("SKIP", "Filen findes allerede"),
            DownloadOutcome.NoUrl           => ("MISS", "Mangler gyldig URL i metadata"),
            DownloadOutcome.TimedOut        => ("TIME", "Afbryd pga. timeout"),
            _                               => ("FAIL", "Fejl (HTTP, IO, timeout, forkert content-type)")
        };

        var extra = result.Message is { Length: > 0 } ? $" - {result.Message}" : "";
        _log($"[{tag}] {id} ({elapsed}) - {human}{extra}");
    }

    private string ElapsedFor(string id)
    {
        if (_startTimes.TryGetValue(id, out var t0))
        {
            var ts = DateTimeOffset.UtcNow - t0;
            return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
        }
        return "00:00";
    }

    private int RentSlot()
    {
        int slot;
        while (!_slotPool.TryDequeue(out slot))
        {
            Thread.SpinWait(50);
        }
        return slot;
    }

    private void ReturnSlot(int slot) => _slotPool.Enqueue(slot);

    public IReadOnlyList<(int Slot, int Count, TimeSpan Total, TimeSpan Average)> GetSlotStats()
    {
        var list = new List<(int, int, TimeSpan, TimeSpan)>();
        foreach (var s in Enumerable.Range(1, _maxConcurrency))
        {
            var totalMs = _slotTotalMs.TryGetValue(s, out var ms) ? ms : 0L;
            var count = _slotCounts.TryGetValue(s, out var c) ? c : 0;
            var total = TimeSpan.FromMilliseconds(totalMs);
            var avg = count > 0 ? TimeSpan.FromMilliseconds(totalMs / (double)count) : TimeSpan.Zero;
            list.Add((s, count, total, avg));
        }
        return list;
    }

    private static byte[] ComputeSHA256(string path)
    {
        using var sha = SHA256.Create();
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return sha.ComputeHash(fs);
    }

    private static string SanitizeFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new char[input.Length];
        var index = 0;
        foreach (var ch in input)
            builder[index++] = invalid.Contains(ch) ? '_' : ch;
        return new string(builder, 0, index);
    }

    // NEW — stream copy med idle-timeout
    private static async Task CopyToAsyncWithIdleTimeout(Stream src, Stream dst, int bufferSize, TimeSpan idleTimeout, CancellationToken ct)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            using var idleCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            while (true)
            {
                idleCts.CancelAfter(idleTimeout);
                int n = await src.ReadAsync(buffer.AsMemory(0, bufferSize), idleCts.Token);
                if (n == 0) break;

                idleCts.CancelAfter(idleTimeout);
                await dst.WriteAsync(buffer.AsMemory(0, n), idleCts.Token);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
