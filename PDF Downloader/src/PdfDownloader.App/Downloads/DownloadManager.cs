using System.Collections.Concurrent;
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

    public DownloadManager(DirectoryInfo outputDirectory, int maxConcurrency, bool skipExisting,
                           bool overwriteDownloads, bool detectChanges, bool keepOldOnChange)
    {
        _outputDirectory = outputDirectory;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PdfDownloader", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/pdf");

        _maxConcurrency = Math.Max(1, maxConcurrency);
        _skipExisting = skipExisting;
        _overwriteDownloads = overwriteDownloads;
        _detectChanges = detectChanges;
        _keepOldOnChange = keepOldOnChange;
    }

    public async Task<IReadOnlyList<DownloadResult>> DownloadAsync(IEnumerable<DownloadRequest> requests, CancellationToken ct)
    {
        Directory.CreateDirectory(_outputDirectory.FullName);

        var results = new ConcurrentBag<DownloadResult>();
        using var throttler = new SemaphoreSlim(_maxConcurrency);

        var tasks = requests.Select(async req =>
        {
            await throttler.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var file = new FileInfo(Path.Combine(_outputDirectory.FullName, SanitizeFileName(req.Id) + ".pdf"));

                if (file.Exists && !_overwriteDownloads && _skipExisting)
                {
                    results.Add(new DownloadResult(req.Id, DownloadOutcome.SkippedExisting, "Already exists", null, file));
                    return;
                }

                // prøv primær → fallback
                foreach (var url in req.Urls)
                {
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    try
                    {
                        using var resp = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                        if (!resp.IsSuccessStatusCode) { continue; }

                        var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
                        if (!contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            // Nogle sites returnerer application/octet-stream – accepter begge
                            if (!string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
                                continue;
                        }

                        // hent til memory/temporær fil for evt. sammenligning
                        var tempPath = Path.GetTempFileName();
                        await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await resp.Content.CopyToAsync(fs, ct).ConfigureAwait(false);
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
                                results.Add(new DownloadResult(req.Id, DownloadOutcome.SkippedExisting, "No change detected", new Uri(url), file));
                                return;
                            }

                            if (changed && _keepOldOnChange)
                            {
                                var updated = Path.Combine(file.Directory!.FullName, Path.GetFileNameWithoutExtension(file.Name) + ".updated.pdf");
                                if (File.Exists(updated))
                                {
                                    // unik suffix for flere opdateringer
                                    updated = Path.Combine(file.Directory!.FullName, Path.GetFileNameWithoutExtension(file.Name) + $".updated-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
                                }
                                File.Move(file.FullName, updated, overwrite: false);
                            }
                        }

                        File.Copy(tempPath, file.FullName, overwrite: true);
                        File.Delete(tempPath);

                        results.Add(new DownloadResult(req.Id, DownloadOutcome.Downloaded, null, new Uri(url), file));
                        return;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // prøv næste URL (fallback)
                        continue;
                    }
                }

                // hvis vi er her: alle forsøg fejlede
                var msg = req.Urls.Count == 0 ? "No URL" : "All URLs failed";
                var outcome = req.Urls.Count == 0 ? DownloadOutcome.NoUrl : DownloadOutcome.Failed;
                results.Add(new DownloadResult(req.Id, outcome, msg, null, null));
            }
            finally
            {
                throttler.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.ToList();
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

    public void Dispose() => _httpClient.Dispose();
}
