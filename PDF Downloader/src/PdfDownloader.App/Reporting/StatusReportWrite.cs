using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PdfDownloader.App.Downloads;

namespace PdfDownloader.App.Reporting;

internal static class StatusReportWriter
{
    public static async Task WriteAsync(FileInfo file, IReadOnlyList<DownloadResult> results, bool append, bool overwrite, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(file.Directory?.FullName ?? Environment.CurrentDirectory);

        if (overwrite && file.Exists)
            file.Delete();

        var writeHeader = !append || !file.Exists;

        using var stream = new FileStream(file.FullName, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = writeHeader
        });

        if (writeHeader)
        {
            csv.WriteField("Id");
            csv.WriteField("Outcome");
            csv.WriteField("Message");
            csv.WriteField("SourceUrl");
            csv.WriteField("SavedFile");
            csv.NextRecord();
        }

        foreach (var result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            csv.WriteField(result.Id);
            csv.WriteField(result.Outcome.ToString());
            csv.WriteField(result.Message ?? string.Empty);
            csv.WriteField(result.SourceUrl?.ToString() ?? string.Empty);
            csv.WriteField(result.SavedFile?.FullName ?? string.Empty);
            csv.NextRecord();
        }

        await writer.FlushAsync().ConfigureAwait(false);
    }
}
