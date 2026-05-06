namespace Pix.ClientDemo.Client.Handlers;

public sealed class IdempotencyKeyHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == HttpMethod.Post &&
            request.RequestUri is not null &&
            request.RequestUri.AbsolutePath.Contains("/pix/v1/cobrancas", StringComparison.OrdinalIgnoreCase) &&
            !request.Headers.Contains("Idempotency-Key"))
        {
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
