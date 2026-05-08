using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Catalogo.Tests.Integration;

public static class AuthHelper
{
    public static async Task<string> ObterTokenAsync(HttpClient client)
    {
        var authBaseUrl = Environment.GetEnvironmentVariable("AUTH_BASE_URL");
        if (!string.IsNullOrWhiteSpace(authBaseUrl))
        {
            using var authClient = new HttpClient { BaseAddress = new Uri(authBaseUrl) };
            var loginResponse = await authClient.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Email = Environment.GetEnvironmentVariable("AUTH_ADMIN_EMAIL") ?? "admin@example.com",
                Senha = Environment.GetEnvironmentVariable("AUTH_ADMIN_PASSWORD") ?? "senha123"
            });

            if (!loginResponse.IsSuccessStatusCode)
                throw new InvalidOperationException($"Auth login failed against AUTH_BASE_URL='{authBaseUrl}': {loginResponse.StatusCode}");

            var tokenResponse = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
            if (string.IsNullOrWhiteSpace(tokenResponse?.Token))
                throw new InvalidOperationException("Auth login returned empty token.");

            return tokenResponse.Token;
        }

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "admin_id"),
            new Claim(JwtRegisteredClaimNames.Email, "admin@example.com"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var secretKey = "MinhaChaveSuperSecretaDePeloMenos32BytesAki123!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "FacShopAPI",
            audience: "TodosOsClientes",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return encoded;
    }

    private sealed record TokenResponse(string Token);
}
