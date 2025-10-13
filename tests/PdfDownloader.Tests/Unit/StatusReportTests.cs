using FluentAssertions;
using PdfDownloader.App.Downloads;
using PdfDownloader.App.Reporting;
using Xunit;

namespace PdfDownloader.Tests.Unit;

public class StatusReportTests
{
    // tests/PdfDownloader.Tests/Unit/StatusReportTests.cs
[Fact]
public async Task Write_And_Read_Roundtrip()
{
    var file = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv"));
    var results = new[]
    {
        new DownloadResult("A1", DownloadOutcome.Downloaded, null, new Uri("https://a"), new FileInfo("A1.pdf")),
        new DownloadResult("A2", DownloadOutcome.Failed, "404", null, null),
    };

    await StatusReportWriter.WriteAsync(file, results, append:false, overwrite:true, CancellationToken.None);

    file.Refresh(); // ← vigtigt: FileInfo cacher Exists
    file.Exists.Should().BeTrue();

    var set = StatusReportReader.LoadCompletedIds(file, CancellationToken.None);
    set.Should().Contain("A1").And.NotContain("A2");
}

}
