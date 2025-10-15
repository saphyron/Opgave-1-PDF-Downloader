using System.Net;

namespace PdfDownloader.Tests.Fakes;

public sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    private readonly Exception _ex;
    public ThrowingHttpMessageHandler(Exception ex) => _ex = ex;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromException<HttpResponseMessage>(_ex);
}
