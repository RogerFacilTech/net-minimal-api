using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Pix.ClientDemo.Client.Handlers;

public sealed class RequestLoggingHandler(ILogger<RequestLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        sw.Stop();

        logger.LogInformation(
            "HTTP {Method} {Path} -> {StatusCode} em {Elapsed}ms",
            request.Method,
            request.RequestUri?.PathAndQuery,
            (int)response.StatusCode,
            sw.ElapsedMilliseconds);

        return response;
    }
}
