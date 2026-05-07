using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FacShopAPI.Shared.Http;

public sealed class RequestLoggingHandler(ILogger<RequestLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        logger.LogInformation(
            "HTTP {Method} {Path} -> {StatusCode} em {ElapsedMs}ms",
            request.Method,
            request.RequestUri?.PathAndQuery,
            (int)response.StatusCode,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
