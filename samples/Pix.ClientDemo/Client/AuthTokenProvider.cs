using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Pix.ClientDemo.Models;

namespace Pix.ClientDemo.Client;

public interface IAuthTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}

public sealed class AuthTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<PixClientOptions> options) : IAuthTokenProvider
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_token) && _expiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _token;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_token) && _expiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _token;
            }

            var client = httpClientFactory.CreateClient("PixServerRaw");
            var request = new OAuthTokenRequest(
                options.Value.ClientId,
                options.Value.ClientSecret,
                "client_credentials");

            var response = await client.PostAsJsonAsync("/oauth/token", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Resposta de token inválida.");

            _token = tokenResponse.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            return _token;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
