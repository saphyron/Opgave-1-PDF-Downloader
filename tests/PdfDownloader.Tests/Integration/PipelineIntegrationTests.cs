using FluentAssertions;
using PdfDownloader.App;
using Xunit;

namespace PdfDownloader.Tests.Integration;

public class PipelineIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Csv_to_statusfile_roundtrip_smoke()
    {
        var csvSample = new FileInfo(Path.Combine(AppContext.BaseDirectory, "samples", "Metadata2006_2016.xlsx")); // eksemplarisk; kører via Excel som anden kilde
        csvSample.Exists.Should().BeTrue();

        var outDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "pipe-" + Guid.NewGuid()));
        outDir.Create();
        var status = new FileInfo(Path.Combine(outDir.FullName, "status.csv"));

        var args = new[]
        {
            "--input", csvSample.FullName,
            "--output", outDir.FullName,
            "--status", status.FullName,
            "--id-column","BRnum",
            "--url-column","Pdf_URL",
            "--limit","5",
            "--max-concurrency","4",
            "--append-status"
        };

        var opts = AppOptions.Parse(args);
        var runner = new ApplicationRunner(opts);
        await runner.RunAsync(CancellationToken.None);

        status.Exists.Should().BeTrue();
        outDir.GetFiles("*.pdf").Length.Should().BeGreaterThan(0, "der bør komme mindst én PDF i smoke-run");
    }
}
