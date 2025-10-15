using FluentAssertions;
using PdfDownloader.App.Reporting;
using Xunit;

namespace PdfDownloader.Tests.Unit;

public class StatusReportReaderReadAllTests
{
    [Fact]
    public void ReadAll_Parses_With_Standard_Header()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        File.WriteAllText(path, "Id,Outcome,Message,SourceUrl,SavedFile\nA,Downloaded,,https://a,A.pdf\nB,Failed,HTTP 404,,\n");

        var rows = StatusReportReader.ReadAll(new FileInfo(path), CancellationToken.None);

        rows.Should().HaveCount(2);
        rows[0].Id.Should().Be("A");
        rows[0].Outcome.Should().Be("Downloaded");
        rows[0].SourceUrl.Should().Be("https://a");
        rows[0].OutputPath.Should().Be("A.pdf");
        rows[1].Outcome.Should().Be("Failed");
        rows[1].Message.Should().Be("HTTP 404");
    }

    [Fact]
    public void ReadAll_Falls_Back_To_Index_When_Header_Names_Differ()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        // Bevidst “forkerte” header-navne (A,B,C,D,E) – ReadAll bør falde tilbage til kolonneindeks
        File.WriteAllText(path, "A,B,C,D,E\nX,Downloaded,,https://x,X.pdf\nY,Failed,Content-Type: text/html,,\n");

        var rows = StatusReportReader.ReadAll(new FileInfo(path), CancellationToken.None);

        rows.Should().HaveCount(2);
        rows[0].Id.Should().Be("X");
        rows[0].Outcome.Should().Be("Downloaded");
        rows[0].SourceUrl.Should().Be("https://x");
        rows[0].OutputPath.Should().Be("X.pdf");
        rows[1].Message.Should().Be("Content-Type: text/html");
    }
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
