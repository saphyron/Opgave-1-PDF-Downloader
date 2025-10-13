using FluentAssertions;
using PdfDownloader.App.Downloads;
using PdfDownloader.Tests.Fakes;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace PdfDownloader.Tests.Unit;

public class DownloadManagerTests
{
    private static HttpResponseMessage PdfOk(byte[]? content = null, string contentType = "application/pdf")
    {
        var resp = new HttpResponseMessage(HttpStatusCode.OK);
        resp.Content = new ByteArrayContent(content ?? new byte[] { 1, 2, 3, 4 });
        resp.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return resp;
    }

    [Fact]
    public async Task Respects_MaxConcurrency()
    {
        var handler = new ConcurrencyProbeHandler(_ => Task.FromResult(PdfOk()));
        var outDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "dm-" + Guid.NewGuid()));
        outDir.Create();

        var mgr = new DownloadManager(outDir, maxConcurrency: 3, skipExisting: false, overwriteDownloads: true,
                                      detectChanges: false, keepOldOnChange: false, httpHandler: handler);

        var reqs = Enumerable.Range(1, 12)
            .Select(i => new DownloadRequest($"X{i}", new[] { $"https://test/{i}.pdf" }))
            .ToList();

        await mgr.DownloadAsync(reqs, CancellationToken.None);

        handler.MaxObservedConcurrency.Should().BeLessOrEqualTo(3);
    }

    [Fact]
    public async Task Skips_NonPdf_ContentType_Except_OctetStream()
    {
        int call = 0;
        var handler = new FakeHttpMessageHandler(req =>
        {
            call++;
            if (call == 1) return Task.FromResult(PdfOk(contentType: "text/html"));       // afvis
            return Task.FromResult(PdfOk(contentType: "application/octet-stream"));       // accepter
        });

        var outDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "dm2-" + Guid.NewGuid()));
        outDir.Create();

        var mgr = new DownloadManager(outDir, 2, false, true, false, false, handler);
        var res = await mgr.DownloadAsync(new[]
        {
            new DownloadRequest("BadThenOk", new[] { "https://a", "https://b" })
        }, CancellationToken.None);

        res.Single().Outcome.Should().Be(DownloadOutcome.Downloaded);
    }

    [Fact]
    public async Task SkipExisting_When_NoOverwrite_And_NoChange()
    {
        // 1) Første løb: skriv en PDF
        var fixedPayload = new byte[] { 9, 9, 9 };
        var handler1 = new FakeHttpMessageHandler(_ => Task.FromResult(PdfOk(fixedPayload)));
        var outDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "dm3-" + Guid.NewGuid()));
        outDir.Create();

        var mgr1 = new DownloadManager(outDir, 1, skipExisting:false, overwriteDownloads:true, detectChanges:false, keepOldOnChange:false, httpHandler: handler1);
        await mgr1.DownloadAsync(new[] { new DownloadRequest("A/\\:*?\"<>|", new[] { "https://x" }) }, CancellationToken.None);

        // 2) Andet løb: samme payload, detectChanges=true, overwriteDownloads=false → SkipExisting(No change)
        var handler2 = new FakeHttpMessageHandler(_ => Task.FromResult(PdfOk(fixedPayload)));
        var mgr2 = new DownloadManager(outDir, 1, skipExisting:false, overwriteDownloads:false, detectChanges:true, keepOldOnChange:false, httpHandler: handler2);
        var res = await mgr2.DownloadAsync(new[] { new DownloadRequest("A/\\:*?\"<>|", new[] { "https://x" }) }, CancellationToken.None);

        res.Single().Outcome.Should().Be(DownloadOutcome.SkippedExisting);
        // Filnavn skal være sanitiseret (ingen invalid chars)
        outDir.GetFiles("A_________.pdf").Should().HaveCount(1);
    }
}
