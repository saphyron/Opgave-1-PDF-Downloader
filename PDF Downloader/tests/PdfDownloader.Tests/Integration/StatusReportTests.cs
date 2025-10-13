using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using PdfDownloader.App.Reporting;
using Xunit;

namespace PdfDownloader.Tests.Integration;

public class StatusReportTests
{
    [Fact]
    public async Task Write_and_read_roundtrip_works()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "PdfDl_Status_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tmp);
        var csv = Path.Combine(tmp, "status.csv");

        await StatusReportWrite.WriteAsync(csv, new[] {
            new PdfDownloader.App.Downloads.DownloadResult("X", PdfDownloader.App.Downloads.DownloadOutcome.Downloaded, "OK")
        }, CancellationToken.None);

        var rows = await StatusReportReader.ReadAsync(csv, CancellationToken.None);
        rows.Should().HaveCount(1);
        rows[0].Id.Should().Be("X");
    }
}
