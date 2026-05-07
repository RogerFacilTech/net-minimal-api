using Microsoft.Extensions.Options;

namespace FacShopAPI.Shared.Http;

public sealed class IdempotencyKeyHandler(IOptions<IdempotencyKeyOptions> options) : DelegatingHandler
{
    private readonly IdempotencyKeyOptions _options = options.Value;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var method = request.Method.Method.ToUpperInvariant();
        var shouldHandleMethod = _options.Methods.Any(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase));

        if (!shouldHandleMethod || request.RequestUri is null)
        {
            return base.SendAsync(request, cancellationToken);
        }

        var pathAndQuery = request.RequestUri.PathAndQuery;
        var shouldHandlePath = _options.PathContains.Count == 0 ||
                               _options.PathContains.Any(path => pathAndQuery.Contains(path, StringComparison.OrdinalIgnoreCase));

        if (!shouldHandlePath || request.Headers.Contains(_options.HeaderName))
        {
            return base.SendAsync(request, cancellationToken);
        }

        request.Headers.Add(_options.HeaderName, Guid.NewGuid().ToString("N"));
        return base.SendAsync(request, cancellationToken);
    }
}
