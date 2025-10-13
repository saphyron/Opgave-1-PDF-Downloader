using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using PdfDownloader.App.Middleware;
using PdfDownloader.App.Reporting;
using Xunit;

namespace PdfDownloader.Tests.Integration;

public class PipelineIntegrationTests
{
    [Fact]
    public async Task Csv_to_statusreport_roundtrip_smoke()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "PdfDl_Int_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tmp);
        var csvPath = Path.Combine(tmp, "input.csv");
        await File.WriteAllTextAsync(csvPath, "Id,Url,FallbackUrl\nID-1,https://example.com/a.pdf,\n");

        // 1) Load metadata (real file, no mocks)
        var loader = new MetadataLoader();
        var rows = await loader.LoadAsync(new FileInfo(csvPath), "Id", "Url", "FallbackUrl", CancellationToken.None);
        rows.Should().HaveCount(1);

        // 2) Write/Read status report roundtrip (real CSV file)
        var outPath = Path.Combine(tmp, "status.csv");
        await StatusReportWrite.WriteAsync(outPath, new[] {
            new PdfDownloader.App.Downloads.DownloadResult("ID-1", PdfDownloader.App.Downloads.DownloadOutcome.Downloaded, "OK")
        }, CancellationToken.None);

        var readBack = await StatusReportReader.ReadAsync(outPath, CancellationToken.None);
        readBack.Should().HaveCount(1);
        readBack.First().Id.Should().Be("ID-1");
    }
}
