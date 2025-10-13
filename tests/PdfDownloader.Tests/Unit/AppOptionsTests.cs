using FluentAssertions;
using PdfDownloader.App;
using Xunit;

namespace PdfDownloader.Tests.Unit;

public class AppOptionsTests
{
    [Fact]
    public void Parse_Defaults_And_Basics()
    {
        var args = new[]
        {
            "--input","in.xlsx","--output",".","--status","status.csv",
            "--id-column","BRnum","--url-column","Pdf_URL"
        };

        var o = AppOptions.Parse(args);

        o.Input.Name.Should().Be("in.xlsx");
        o.Output.Exists.Should().BeTrue();
        o.StatusReport!.Name.Should().Be("status.csv");
        o.SkipExisting.Should().BeTrue();           // default
        o.AppendStatus.Should().BeTrue();           // default
        o.OverwriteStatus.Should().BeFalse();
        o.MaxConcurrency.Should().Be(10);           // default
    }

    [Fact]
    public void Parse_MutuallyExclusive_StatusFlags()
    {
        var args = new[]
        {
            "--input","in.xlsx","--output",".",
            "--overwrite-status","--append-status"
        };

        Action act = () => AppOptions.Parse(args);
        act.Should().Throw<OptionParsingException>()
           .WithMessage("*Vælg enten --append-status eller --overwrite-status*");
    }
}
