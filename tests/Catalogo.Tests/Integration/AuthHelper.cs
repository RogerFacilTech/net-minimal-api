using System.Net.Http.Json;

namespace Catalogo.Tests.Integration;

public static class AuthHelper
{
    public static async Task<string> ObterTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "admin@example.com",
            Senha = "senha123"
        });

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Login failed: {response.StatusCode}");

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return result!.Token;
    }

    private record TokenResponse(string Token);
}
