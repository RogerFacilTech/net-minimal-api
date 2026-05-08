using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FacShopAPI.Catalogo.Endpoints.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace FacShopAPI.Catalogo.Endpoints.Endpoints.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/login", Login).WithName("UserLogin")
            .Accepts<LoginRequest>("application/json")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
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
}
