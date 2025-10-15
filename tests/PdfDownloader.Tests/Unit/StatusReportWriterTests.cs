using FluentAssertions;
using PdfDownloader.App.Downloads;
using PdfDownloader.App.Reporting;
using Xunit;

namespace PdfDownloader.Tests.Unit;

public class StatusReportWriterTests
{
    [Fact]
    public async Task Append_Writes_Header_Once()
    {
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv"));

        var r1 = new[]
        {
            new DownloadResult("A1", DownloadOutcome.Downloaded, null, new Uri("https://a"), new FileInfo("A1.pdf"))
        };
        var r2 = new[]
        {
            new DownloadResult("B1", DownloadOutcome.Failed, "HTTP 404", null, null)
        };

        await StatusReportWriter.WriteAsync(file, r1, append:false, overwrite:true, CancellationToken.None);
        await StatusReportWriter.WriteAsync(file, r2, append:true,  overwrite:false, CancellationToken.None);

        var lines = File.ReadAllLines(file.FullName);
        lines.Should().HaveCount(1 /*header*/ + 2 /*rows*/);
        lines[0].Should().StartWith("Id,Outcome,Message,SourceUrl,SavedFile");
    }
}
