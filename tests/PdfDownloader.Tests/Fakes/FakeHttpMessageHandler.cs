using System.Net;
using System.Net.Http.Headers;
using System.Collections.Concurrent;

namespace PdfDownloader.Tests.Fakes;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _impl;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> impl)
        => _impl = impl;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _impl(request);
}

internal sealed class ConcurrencyProbeHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _impl;
    public int MaxObservedConcurrency => _maxObserved;
    private int _current;
    private int _maxObserved;

    public ConcurrencyProbeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> impl)
        => _impl = impl;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var now = Interlocked.Increment(ref _current);
        InterlockedExtensions.Max(ref _maxObserved, now);
        try
        {
            // Simuler netværkslatens
            await Task.Delay(150, cancellationToken);
            return await _impl(request);
        }
        finally
        {
            Interlocked.Decrement(ref _current);
        }
    }

    private static class InterlockedExtensions
    {
        public static void Max(ref int target, int value)
        {
            int initial, computed;
            do
            {
                initial = target;
                computed = Math.Max(initial, value);
            } while (Interlocked.CompareExchange(ref target, computed, initial) != initial);
        }
    }
}
