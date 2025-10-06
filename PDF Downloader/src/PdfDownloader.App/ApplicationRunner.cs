using PdfDownloader.App.Downloads;
using PdfDownloader.App.Middleware;
using PdfDownloader.App.Reporting;

namespace PdfDownloader.App;

internal sealed class ApplicationRunner(AppOptions options)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loader = new MetadataLoader();
        var records = await loader.LoadAsync(options.Input, options.IdColumn, options.UrlColumn, options.FallbackUrlColumn, cancellationToken);

        if (records.Count == 0)
        {
            Console.WriteLine("Ingen rækker med gyldige id'er blev fundet i inputfilen.");
            return;
        }

        var candidates = records
            .Where(r => r.HasAnyUrl)
            .Take(options.Limit)
            .ToList();

        if (candidates.Count == 0)
        {
            Console.WriteLine("Ingen rækker havde en gyldig URL.");
            return;
        }

        var requests = candidates
            .Select(r => new DownloadRequest(r.Id, r.GetOrderedUrls()))
            .ToList();

        Console.WriteLine($"Starter download af {requests.Count} rapporter til '{options.Output.FullName}'.");

        using var manager = new DownloadManager(options.Output, options.MaxConcurrency, options.SkipExisting);
        var results = await manager.DownloadAsync(requests, cancellationToken);

        var downloaded = results.Count(r => r.Outcome == DownloadOutcome.Downloaded);
        var skipped = results.Count(r => r.Outcome == DownloadOutcome.SkippedExisting);
        var failed = results.Count(r => r.Outcome == DownloadOutcome.Failed);
        var missing = results.Count(r => r.Outcome == DownloadOutcome.NoUrl);

        Console.WriteLine();
        Console.WriteLine("Opsummering:");
        Console.WriteLine($"  Hentet: {downloaded}");
        Console.WriteLine($"  Skippet (allerede hentet): {skipped}");
        Console.WriteLine($"  Fejlet: {failed}");
        Console.WriteLine($"  Ingen URL: {missing}");

        if (options.StatusReport is { } statusFile)
        {
            await StatusReportWriter.WriteAsync(statusFile, results, cancellationToken);
            Console.WriteLine($"Statusrapport gemt i {statusFile.FullName}");
        }
    }
}