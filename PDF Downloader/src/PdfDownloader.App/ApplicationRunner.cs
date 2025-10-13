using System.Text;
using PdfDownloader.App.Downloads;
using PdfDownloader.App.Middleware;
using PdfDownloader.App.Reporting;

namespace PdfDownloader.App;

internal sealed class ApplicationRunner(AppOptions options)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var runStart = DateTimeOffset.Now;
        var stamp = runStart.ToString("yyyy-MM-dd_HHmmss");

        // Hvor skal run-loggen ligge? Samme mappe som status.csv, ellers i Output.
        var runBaseDir = options.StatusReport?.Directory ?? options.Output;
        Directory.CreateDirectory(runBaseDir.FullName);

        var runLogPath = Path.Combine(runBaseDir.FullName, $"run_{stamp}.log");

        // Trådsikker writer til logfil + spejling til konsol
        using var fs = new FileStream(runLogPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = true };
        var syncWriter = TextWriter.Synchronized(writer);
        void Log(string line)
        {
            Console.WriteLine(line);
            syncWriter.WriteLine(line);
        }

        Log($"RUN START: {runStart:yyyy-MM-dd HH:mm:ss}");
        Log($"Input: {options.Input.FullName}");
        Log($"Output: {options.Output.FullName}");
        if (options.StatusReport is not null) Log($"Status CSV: {options.StatusReport.FullName}");
        Log($"Max concurrency: {options.MaxConcurrency}");
        Log($"Limit: {options.Limit}");
        Log("");

        var loader = new MetadataLoader();
        var records = await loader.LoadAsync(options.Input, options.IdColumn, options.UrlColumn, options.FallbackUrlColumn, cancellationToken);

        if (records.Count == 0)
        {
            Log("Ingen metadata fundet.");
            var runEndEmpty = DateTimeOffset.Now;
            Log($"RUN END: {runEndEmpty:yyyy-MM-dd HH:mm:ss} (varighed: {runEndEmpty - runStart:g})");
            return;
        }

        // 1) Resume: læs status-CSV for allerede hentede
        var alreadyDone = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (options.ResumeFromStatus is { } resumeFile)
        {
            alreadyDone = StatusReportReader.LoadCompletedIds(resumeFile, cancellationToken);
            Log($"Resume: {alreadyDone.Count} IDs markeret som Downloaded i {resumeFile.Name}.");
        }

        // 2) Filtrér
        var filtered = records
            .Where(r => r.HasAnyUrl)
            .Where(r => !alreadyDone.Contains(r.Id))
            .ToList();

        // 3) Udvælgelses-politik
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
            Log("Ingen rækker at behandle efter filtre.");
            var runEndNoRows = DateTimeOffset.Now;
            Log($"RUN END: {runEndNoRows:yyyy-MM-dd HH:mm:ss} (varighed: {runEndNoRows - runStart:g})");
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
            keepOldOnChange: options.KeepOldOnChange,
            logger: Log // <- alt der logges fra DownloadManager går også i filen
        );

        var results = await mgr.DownloadAsync(requests, cancellationToken);

        // 5) Opsummering (denne kørsel)
        var downloaded = results.Count(r => r.Outcome == DownloadOutcome.Downloaded);
        var skipped    = results.Count(r => r.Outcome == DownloadOutcome.SkippedExisting);
        var failed     = results.Count(r => r.Outcome == DownloadOutcome.Failed);
        var missing    = results.Count(r => r.Outcome == DownloadOutcome.NoUrl);

        Log("");
        Log("Opsummering (denne kørsel):");
        Log($"  Hentet: {downloaded}");
        Log($"  Skippet (allerede hentet/ingen ændring): {skipped}");
        Log($"  Fejlet: {failed}");
        Log($"  Ingen URL: {missing}");

        // Fejl fordelt på årsag (baseret på Result.Message)
        var reasonGroups = results
            .Where(r => r.Outcome == DownloadOutcome.Failed)
            .GroupBy(r =>
            {
                var m = r.Message ?? "";
                if (m.StartsWith("HTTP ")) return m;                       // fx "HTTP 404"
                if (m.StartsWith("Content-Type:")) return m;               // fx "Content-Type: text/html"
                if (m.StartsWith("Timeout")) return "Timeout";
                if (m.StartsWith("Exception:")) return m;                  // fx "Exception: HttpRequestException"
                return string.IsNullOrWhiteSpace(m) ? "Ukendt" : m;
            })
            .OrderByDescending(g => g.Count())
            .ToList();

        if (reasonGroups.Count > 0)
        {
            Log("  Fejl fordelt:");
            foreach (var g in reasonGroups)
                Log($"    - {g.Key}: {g.Count()}");
        }

        // 6) Status-CSV
        if (options.StatusReport is { } statusFile)
        {
            await StatusReportWriter.WriteAsync(statusFile, results, append: options.AppendStatus, overwrite: options.OverwriteStatus, cancellationToken);
            Log($"Statusrapport {(options.AppendStatus ? "appendet" : options.OverwriteStatus ? "overskrevet" : "skrevet")} i {statusFile.FullName}");
        }

        // 7) Gennemsnit pr. tråd (bevis for concurrency)
        var stats = mgr.GetSlotStats()
                       .OrderBy(s => s.Slot)
                       .ToList();
        if (stats.Count > 0)
        {
            Log("");
            Log("Trådstatistik (gennemsnit pr. tråd):");
            foreach (var s in stats)
            {
                Log($"  Tråd #{s.Slot}: jobs={s.Count}, total={FormatSpan(s.Total)}, avg={FormatSpan(s.Average)}");
            }
        }

        var runEnd = DateTimeOffset.Now;
        Log("");
        Log($"RUN END: {runEnd:yyyy-MM-dd HH:mm:ss} (varighed: {runEnd - runStart:g})");
    }

    private static string FormatSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
        return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
    }
}
