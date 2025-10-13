using System.Net;
using System.Net.Http;
using FluentAssertions;
using PdfDownloader.Tests.TestHelpers;
using Xunit;

namespace PdfDownloader.Tests.Unit;

public class DownloadManagerTests
{
    [Fact]
    public void Placeholder_until_public_api_is_exposed()
    {
        // NOTE: Replace this with calls to your real DownloadManager once the API surface is accessible from tests.
        // For nu: tjek at helpers kan instansieres.
        using var _ = new TempDir();
        var handler = new HttpMessageHandlerStub(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler);
        client.Should().NotBeNull();
    }
}
