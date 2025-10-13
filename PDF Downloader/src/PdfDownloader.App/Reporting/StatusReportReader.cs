// ./PDF Downloader/src/PdfDownloader.App/Reporting/StatusReportReader.cs
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

        // Læs første række + header før vi tilgår felter ved navn
        if (!csv.Read())
            return set;

        csv.ReadHeader();

        while (csv.Read())
        {
            ct.ThrowIfCancellationRequested();

            var id      = SafeGet(csv, "Id",        0);
            var outcome = SafeGet(csv, "Outcome",   1);

            if (!string.IsNullOrWhiteSpace(id) &&
                outcome?.Equals(DownloadOutcome.Downloaded.ToString(), StringComparison.OrdinalIgnoreCase) == true)
            {
                set.Add(id!);
            }
        }
        return set;
    }

    /// <summary>
    /// Læser ALLE rækker i status-CSV’en som StatusRow (robust over for header-navne og evt. manglende/ændrede kolonner).
    /// </summary>
    public static IReadOnlyList<StatusRow> ReadAll(FileInfo statusFile, CancellationToken ct)
    {
        var list = new List<StatusRow>();
        if (!statusFile.Exists) return list;

        using var reader = new StreamReader(statusFile.FullName);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
            HeaderValidated = null,
            MissingFieldFound = null
        });

        // Hvis filen er tom
        if (!csv.Read())
            return list;

        // Prøv at læse header – hvis ingen header, fortsætter vi og falder tilbage til kolonneindeks
        try { csv.ReadHeader(); } catch { /* ignore */ }

        while (csv.Read())
        {
            ct.ThrowIfCancellationRequested();

            var id        = SafeGet(csv, "Id",        0) ?? string.Empty;
            var outcome   = SafeGet(csv, "Outcome",   1) ?? string.Empty;
            var message   = SafeGet(csv, "Message",   2);
            var sourceUrl = SafeGet(csv, "SourceUrl", 3);
            var savedFile = SafeGet(csv, "SavedFile", 4);

            // Map 'SavedFile' til recordens OutputPath
            list.Add(new StatusRow(id, outcome,
                                   string.IsNullOrWhiteSpace(message) ? null : message,
                                   string.IsNullOrWhiteSpace(sourceUrl) ? null : sourceUrl,
                                   string.IsNullOrWhiteSpace(savedFile) ? null : savedFile));
        }

        return list;
    }

    private static string? SafeGet(CsvReader csv, string name, int index)
    {
        try
        {
            if (csv.TryGetField(name, out string? byName))
                return byName;
        }
        catch { /* ignore and fall back */ }

        try
        {
            if (csv.TryGetField(index, out string? byIndex))
                return byIndex;
        }
        catch { /* ignore */ }

        return null;
    }
}
