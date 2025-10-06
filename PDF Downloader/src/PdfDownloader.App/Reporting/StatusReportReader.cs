using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using PdfDownloader.App.Downloads;

namespace PdfDownloader.App.Reporting;

internal static class StatusReportReader
{
    public sealed record StatusRow(string Id, string Outcome, string? Message, string? SourceUrl, string? OutputPath);

    public static HashSet<string> LoadCompletedIds(FileInfo statusFile, CancellationToken ct)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!statusFile.Exists) return set;

        using var reader = new StreamReader(statusFile.FullName);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null
        });

        while (csv.Read())
        {
            ct.ThrowIfCancellationRequested();
            var id = csv.GetField("Id") ?? csv.GetField(0);
            var outcome = csv.GetField("Outcome") ?? csv.GetField(1);
            if (!string.IsNullOrWhiteSpace(id) && string.Equals(outcome, DownloadOutcome.Downloaded.ToString(), StringComparison.OrdinalIgnoreCase))
                set.Add(id!);
        }
        return set;
    }
}
