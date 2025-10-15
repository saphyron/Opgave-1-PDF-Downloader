using FluentAssertions;
using PdfDownloader.App.Middleware;
using Xunit;

namespace PdfDownloader.Tests.Unit;

public class MetadataRecordTests
{
    [Fact]
    public void HasAnyUrl_Is_True_When_Primary()
    {
        var r = new MetadataRecord("A1", "https://p", null);
        r.HasAnyUrl.Should().BeTrue();
    }

    [Fact]
    public void HasAnyUrl_Is_True_When_Fallback()
    {
        var r = new MetadataRecord("A1", null, "https://f");
        r.HasAnyUrl.Should().BeTrue();
    }

    [Fact]
    public void HasAnyUrl_Is_False_When_None()
    {
        var r = new MetadataRecord("A1", null, null);
        r.HasAnyUrl.Should().BeFalse();
    }

    [Fact]
    public void GetOrderedUrls_Primary_Then_Fallback()
    {
        var r = new MetadataRecord("A1", "https://p", "https://f");
        var list = r.GetOrderedUrls();
        list.Should().ContainInOrder("https://p", "https://f");
    }

    [Fact]
    public void GetOrderedUrls_Only_Existing()
    {
        var r = new MetadataRecord("A1", null, "https://f");
        r.GetOrderedUrls().Should().BeEquivalentTo(new[] { "https://f" });
    }
}
