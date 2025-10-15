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

    [Fact]
    public void NoSkipExisting_Flag_Disables_Skip()
    {
        var args = new[]
        {
            "--input","in.xlsx","--output",".",
            "--no-skip-existing"
        };
        var o = AppOptions.Parse(args);
        o.SkipExisting.Should().BeFalse();
    }

    [Fact]
    public void KeepOldOnChange_Is_Implied_By_OverwriteDownloads()
    {
        var args = new[]
        {
            "--input","in.xlsx","--output",".",
            "--overwrite-downloads"
        };
        var o = AppOptions.Parse(args);
        o.OverwriteDownloads.Should().BeTrue();
        o.KeepOldOnChange.Should().BeTrue(); // implikation i parseren
    }

    [Fact]
    public void Unknown_Extension_Throws()
    {
        var args = new[]
        {
            "--input","in.xyz","--output","."
        };
        Action act = () => AppOptions.Parse(args);
        act.Should().NotThrow(); // parser tillader filnavn hvad som helst…

        // … men selve loaderen vil kaste – det testes i MetadataLoader
    }
}