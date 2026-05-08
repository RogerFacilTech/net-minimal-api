using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using FacShopAPI.Shared.Web;

namespace Auth.Endpoints;

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/login", Login)
            .WithName("UserLogin")
            .Accepts<LoginRequest>("application/json")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithTags("Auth")
            .AllowAnonymous();
    }

    private static IResult Login(LoginRequest req, IConfiguration configuration)
    {
        var adminEmail = configuration["Auth:AdminEmail"];
        var adminPassword = configuration["Auth:AdminPassword"];
        if (req.Email != adminEmail || req.Senha != adminPassword)
            return Results.Unauthorized();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "admin_id"),
            new Claim(JwtRegisteredClaimNames.Email, req.Email),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var secretKey = configuration["Jwt:Key"] ?? "MinhaChaveSuperSecretaDePeloMenos32BytesAki123!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "FacShopAPI",
            audience: configuration["Jwt:Audience"] ?? "TodosOsClientes",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return Results.Ok(new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresIn = (int)TimeSpan.FromHours(2).TotalSeconds
        });
    }

    public record LoginRequest(string Email, string Senha);
    public record AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }
}
