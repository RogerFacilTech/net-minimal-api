using System.Text;
using FacShopAPI.Shared.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/auth-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "MinhaChaveSuperSecretaDePeloMenos32BytesAki123!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FacShopAPI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TodosOsClientes",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// Endpoints
builder.Services.AddEndpointsFromAssembly(typeof(Auth.Endpoints.LoginEndpoint).Assembly);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auth API",
        Version = "v1.0.0",
        Description = "API de Autenticação (JWT) - Projeto Educacional"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapRegisteredEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .AllowAnonymous();

app.Run();

public partial class Program { }
