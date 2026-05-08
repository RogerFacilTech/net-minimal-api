using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Catalogo.Tests.Integration;

public static class AuthHelper
{
    private const string DefaultIssuer = "FacShopAPI";
    private const string DefaultAudience = "TodosOsClientes";

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

            EnsureTokenLooksValid(tokenResponse.Token);

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
            issuer: DefaultIssuer,
            audience: DefaultAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return encoded;
    }

    private static void EnsureTokenLooksValid(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
            throw new InvalidOperationException("Auth login returned an invalid JWT format.");

        var jwt = handler.ReadJwtToken(token);
        var expectedIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? DefaultIssuer;
        var expectedAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? DefaultAudience;

        if (!string.Equals(jwt.Issuer, expectedIssuer, StringComparison.Ordinal))
            throw new InvalidOperationException($"Auth login returned token with invalid issuer '{jwt.Issuer}'. Expected '{expectedIssuer}'.");

        if (!jwt.Audiences.Contains(expectedAudience, StringComparer.Ordinal))
            throw new InvalidOperationException($"Auth login returned token without expected audience '{expectedAudience}'.");

        if (jwt.ValidTo <= DateTime.UtcNow)
            throw new InvalidOperationException("Auth login returned an already expired token.");
    }

    private sealed record TokenResponse(string Token);
}
