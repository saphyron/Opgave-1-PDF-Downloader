using FluentAssertions;
using PdfDownloader.App.Middleware;
using ClosedXML.Excel;
using Xunit;

namespace PdfDownloader.Tests.Unit;

public class MetadataLoaderTests
{
    [Fact]
    public async Task Load_From_Csv_With_Fallback()
    {
        var csv = "BRnum,Pdf_URL,Alt\nA1,https://primary/a.pdf,https://fallback/a.pdf\nA2,,https://fallback/b.pdf\n";
        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, csv);
        var file = new FileInfo(tmp);
        File.Move(tmp, tmp + ".csv");
        file = new FileInfo(tmp + ".csv");

        var loader = new MetadataLoader();
        var rows = await loader.LoadAsync(file, "BRnum", "Pdf_URL", "Alt", CancellationToken.None);

        rows.Should().HaveCount(2);
        rows[0].Id.Should().Be("A1");
        rows[0].PrimaryUrl.Should().NotBeNull();
        rows[0].FallbackUrl.Should().NotBeNull();
        rows[1].Id.Should().Be("A2");
        rows[1].PrimaryUrl.Should().BeNull();
        rows[1].FallbackUrl.Should().NotBeNull();
    }

    [Fact]
    public async Task Load_From_Excel_Maps_Headers_CaseInsensitive()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xlsx");
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            ws.Cell(1,1).Value = "brNUM";
            ws.Cell(1,2).Value = "pdf_url";
            ws.Cell(2,1).Value = "A1";
            ws.Cell(2,2).Value = "https://site/a.pdf";
            wb.SaveAs(path);
        }

        var loader = new MetadataLoader();
        var rows = await loader.LoadAsync(new FileInfo(path), "BRnum", "Pdf_URL", null, CancellationToken.None);

        rows.Should().ContainSingle();
        rows[0].Id.Should().Be("A1");
        rows[0].PrimaryUrl.Should().Be("https://site/a.pdf");
    }
}
