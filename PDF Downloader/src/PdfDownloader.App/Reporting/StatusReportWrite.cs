using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PdfDownloader.App.Downloads;

namespace PdfDownloader.App.Reporting;

internal static class StatusReportWriter
{
    public static async Task WriteAsync(FileInfo file, IReadOnlyList<DownloadResult> results, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(file.Directory?.FullName ?? Environment.CurrentDirectory);

        await using var stream = file.Open(FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        using var csv = new CsvWriter(writer, config);
        csv.WriteField("Id");
        csv.WriteField("Status");
        csv.WriteField("Message");
        csv.WriteField("SourceUrl");
        csv.WriteField("OutputPath");
        csv.NextRecord();

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