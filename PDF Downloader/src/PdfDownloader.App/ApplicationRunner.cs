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
            Console.WriteLine("Ingen metadata fundet.");
            return;
        }

        // 1) Resume: læs status-CSV for allerede hentede
        var alreadyDone = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (options.ResumeFromStatus is { } resumeFile)
        {
            alreadyDone = StatusReportReader.LoadCompletedIds(resumeFile, cancellationToken);
            Console.WriteLine($"Resume: {alreadyDone.Count} IDs markeret som Downloaded i {resumeFile.Name}.");
        }

        // 2) Filtrér records ud fra resume + skipExisting flag
        var filtered = records
            .Where(r => r.HasAnyUrl)
            .Where(r => !alreadyDone.Contains(r.Id)) // spring dem over som allerede er marked ‘Downloaded’
            .ToList();

        // 3) Udvælgelses-politik (first / range / skip-take / limit)
        IEnumerable<Middleware.MetadataRecord> selected = filtered;

        if (options.First is { } firstN && firstN > 0)
            selected = selected.Take(firstN);
        if (options.FromIndex is { } from && from > 0)
            selected = selected.Skip(from - 1);
        if (options.ToIndex is { } to && to > 0)
            selected = selected.Take(Math.Max(0, to - (options.FromIndex ?? 1) + 1));
        if (options.Skip is { } sk && sk > 0)
            selected = selected.Skip(sk);
        if (options.Take is { } tk && tk > 0)
            selected = selected.Take(tk);
        if (options.Limit > 0)
            selected = selected.Take(options.Limit);

        var finalList = selected.ToList();
        if (finalList.Count == 0)
        {
            Console.WriteLine("Ingen rækker at behandle efter filtre.");
            return;
        }

        // 4) Download
        var requests = finalList.Select(r => new DownloadRequest(r.Id, r.GetOrderedUrls())).ToList();

        var mgr = new DownloadManager(
            options.Output,
            options.MaxConcurrency,
            skipExisting: options.SkipExisting,
            overwriteDownloads: options.OverwriteDownloads,
            detectChanges: options.DetectChanges,
            keepOldOnChange: options.KeepOldOnChange
        );

        var results = await mgr.DownloadAsync(requests, cancellationToken);

        // 5) Opsummering
        var downloaded = results.Count(r => r.Outcome == DownloadOutcome.Downloaded);
        var skipped    = results.Count(r => r.Outcome == DownloadOutcome.SkippedExisting);
        var failed     = results.Count(r => r.Outcome == DownloadOutcome.Failed);
        var missing    = results.Count(r => r.Outcome == DownloadOutcome.NoUrl);

        Console.WriteLine();
        Console.WriteLine("Opsummering:");
        Console.WriteLine($"  Hentet: {downloaded}");
        Console.WriteLine($"  Skippet (allerede hentet/ingen ændring): {skipped}");
        Console.WriteLine($"  Fejlet: {failed}");
        Console.WriteLine($"  Ingen URL: {missing}");

        // 6) Skriv status (append/overwrite)
        if (options.StatusReport is { } statusFile)
        {
            await StatusReportWriter.WriteAsync(statusFile, results, append: options.AppendStatus, overwrite: options.OverwriteStatus, cancellationToken);
            Console.WriteLine($"Statusrapport {(options.AppendStatus ? "appendet" : options.OverwriteStatus ? "overskrevet" : "skrevet")} i {statusFile.FullName}");
        }
    }
}
