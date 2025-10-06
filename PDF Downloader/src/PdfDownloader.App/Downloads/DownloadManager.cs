using System.Collections.Concurrent;
using System.Net.Http.Headers;

namespace PdfDownloader.App.Downloads;

internal sealed class DownloadManager : IDisposable
{
    private readonly DirectoryInfo _outputDirectory;
    private readonly HttpClient _httpClient;
    private readonly int _maxConcurrency;
    private readonly bool _skipExisting;

    public DownloadManager(DirectoryInfo outputDirectory, int maxConcurrency, bool skipExisting)
    {
        _outputDirectory = outputDirectory;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120),
        };
        _maxConcurrency = Math.Max(1, maxConcurrency);
        _skipExisting = skipExisting;
    }

    public async Task<IReadOnlyList<DownloadResult>> DownloadAsync(IEnumerable<DownloadRequest> requests, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_outputDirectory.FullName);

        var results = new ConcurrentBag<DownloadResult>();
        using var semaphore = new SemaphoreSlim(_maxConcurrency);

        var tasks = requests.Select(async request =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await DownloadSingleAsync(request, cancellationToken).ConfigureAwait(false);
                results.Add(result);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return results
            .OrderBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<DownloadResult> DownloadSingleAsync(DownloadRequest request, CancellationToken cancellationToken)
    {
        if (request.Urls.Count == 0)
        {
            return new DownloadResult(request.Id, DownloadOutcome.NoUrl, "Ingen URL angivet.", null, null);
        }

        var targetFile = new FileInfo(Path.Combine(_outputDirectory.FullName, SanitizeFileName(request.Id) + ".pdf"));

        if (_skipExisting && targetFile.Exists)
        {
            return new DownloadResult(request.Id, DownloadOutcome.SkippedExisting, "Allerede hentet.", null, targetFile);
        }

        string? lastError = null;

        foreach (var url in request.Urls)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                lastError = "Ugyldig URL.";
                continue;
            }

            try
            {
                using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    lastError = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                    continue;
                }

                if (!IsPdf(response.Content.Headers))
                {
                    lastError = "Indholdet er ikke en PDF.";
                    continue;
                }

                Directory.CreateDirectory(targetFile.Directory!.FullName);

                await using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using var fileStream = targetFile.Open(FileMode.Create, FileAccess.Write, FileShare.None);
                await networkStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);

                return new DownloadResult(request.Id, DownloadOutcome.Downloaded, null, uri, targetFile);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                lastError = ex.Message;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }
        }

        return new DownloadResult(request.Id, DownloadOutcome.Failed, lastError ?? "Ukendt fejl.", null, null);
    }

    private static bool IsPdf(HttpContentHeaders headers)
    {
        if (headers.ContentType is { } contentType && contentType.MediaType is { } mediaType)
        {
            if (mediaType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (headers.ContentDisposition?.FileNameStar is { } star && star.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (headers.ContentDisposition?.FileName is { } fileName && fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return headers.ContentType is null;
    }

    private static string SanitizeFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new char[input.Length];
        var index = 0;
        foreach (var ch in input)
        {
            builder[index++] = invalid.Contains(ch) ? '_' : ch;
        }

        return new string(builder, 0, index);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}