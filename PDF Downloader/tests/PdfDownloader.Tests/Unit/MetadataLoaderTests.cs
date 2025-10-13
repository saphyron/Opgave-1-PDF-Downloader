using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using PdfDownloader.App.Middleware;
using Xunit;

namespace PdfDownloader.Tests.Unit;

public class MetadataLoaderTests
{
    [Fact]
    public async Task LoadAsync_Csv_reads_rows_and_maps_columns()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "PdfDl_Unit_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tmp);
        var csvPath = Path.Combine(tmp, "input.csv");
        await File.WriteAllTextAsync(csvPath, "Id,Url,FallbackUrl\nA,https://example/a.pdf,\nB,,https://example/b.pdf\n");

        var loader = new MetadataLoader();
        var rows = await loader.LoadAsync(new FileInfo(csvPath), "Id", "Url", "FallbackUrl", CancellationToken.None);

        rows.Should().HaveCount(2);
        rows.Select(r => r.Id).Should().BeEquivalentTo(new[] { "A", "B" });
        rows.First(r => r.Id == "A").Url.Should().Be("https://example/a.pdf");
        rows.First(r => r.Id == "B").FallbackUrl.Should().Be("https://example/b.pdf");
    }

    [Fact]
    public async Task LoadAsync_Unknown_extension_throws()
    {
        var path = Path.Combine(Path.GetTempPath(), "input.unknown");
        await File.WriteAllTextAsync(path, "dummy");
        var loader = new MetadataLoader();
        var act = () => loader.LoadAsync(new FileInfo(path), "Id", "Url", null, CancellationToken.None);
        await act.Should().ThrowAsync<System.Exception>();
    }
}
