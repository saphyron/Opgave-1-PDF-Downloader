using FluentAssertions;
using PdfDownloader.App;
using PdfDownloader.App.Downloads;
using PdfDownloader.App.Reporting;
using Xunit;

namespace PdfDownloader.Tests.Integration;

public class ExcelIntegrationTests
{
    [Fact]
    [Trait("Category","Integration")]
    public async Task MetadataLoader_reads_real_excel_and_maps_BRnum_and_PdfURL()
    {
        var sample = new FileInfo(Path.Combine(AppContext.BaseDirectory, "samples", "GRI_2017_2020 (1).xlsx"));
        sample.Exists.Should().BeTrue("sample Excel should be present");

        // Brug ApplicationRunner for at teste hele vejen
        var output = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "pdf-out-" + Guid.NewGuid()));
        var status = new FileInfo(Path.Combine(output.FullName, "status.csv"));
        output.Create();

        var args = new[]
        {
            "--input", sample.FullName,
            "--output", output.FullName,
            "--status", status.FullName,
            "--id-column","BRnum",
            "--url-column","Pdf_URL",
            "--limit","5",
            "--max-concurrency","3",
            "--detect-changes"              // tåler reruns
        };

        var opts = AppOptions.Parse(args);
        var runner = new ApplicationRunner(opts);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await runner.RunAsync(cts.Token);

        // Verificér statusfil
        status.Exists.Should().BeTrue();
        var doneIds = StatusReportReader.LoadCompletedIds(status, CancellationToken.None);

        // Krav: "konstatere at der blev hentet nogle PDF'er" (live data → tillad også at tidligere dl er markeret)
        // Vi accepterer enten netop hentede filer eller tidligere hentede (no-change/skipExisting scenarie)
        var anyPdfOnDisk = output.GetFiles("*.pdf", SearchOption.TopDirectoryOnly).Any();
        (anyPdfOnDisk || doneIds.Count > 0).Should().BeTrue("mindst én PDF burde være (ny) hentet eller markeret i status");
    }
}
