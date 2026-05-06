using System.Text;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Endpoints.Endpoints.Atributos;
using FacShopAPI.Catalogo.Endpoints.Endpoints.Auth;
using FacShopAPI.Catalogo.Endpoints.Endpoints.Categorias;
using FacShopAPI.Catalogo.Endpoints.Endpoints.Midias;
using FacShopAPI.Catalogo.Endpoints.Endpoints.Produtos;
using FacShopAPI.Catalogo.Endpoints.Endpoints.Variantes;
using FacShopAPI.Catalogo.Endpoints.Extensions;
using FacShopAPI.Shared.Data;
using FacShopAPI.Shared.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// CONFIGURAÇÃO DE LOGGING
// ==========================================

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/api-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        fileSizeLimitBytes: 10_000_000,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// ==========================================
// CONFIGURAÇÃO DE BANCO DE DADOS
// ==========================================

if (builder.Environment.IsEnvironment("Testing"))
{
    var testDbName = $"TestDb_{Guid.NewGuid():N}.db";
    var testDbPath = Path.Combine(Path.GetTempPath(), testDbName);
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={testDbPath}"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=produtos-api.db";

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}

// ==========================================
// CONFIGURAÇÃO DE DEPENDENCY INJECTION
// ==========================================

// Conectar AppDbContext → ICatalogoContext para injeção de dependência do repositório
builder.Services.AddScoped<ICatalogoContext>(sp => sp.GetRequiredService<AppDbContext>());

// Registrar todos os serviços do bounded context Catálogo
builder.Services.AddCatalogo();

// Rate limiting — não registrar em Testing (ApiFactory/RateLimitingApiFactory registram com limites próprios)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddCatalogoRateLimiting();
}



// ==========================================
// CONFIGURAÇÃO DE MAPEAMENTO
// ==========================================

builder.Services.AddAutoMapper(_ => { }, typeof(FacShopAPI.Catalogo.Application.Mappings.ProdutoMappingProfile).Assembly);

// ==========================================
// CONFIGURAÇÃO DE CORS
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Cache em memória para armazenar as chaves de idempotência
builder.Services.AddMemoryCache();

// ==========================================
// CONFIGURAÇÃO DE SEGURANÇA (JWT)
// ==========================================

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
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ProdutosAPI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TodosOsClientes",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ==========================================
// CONFIGURAÇÃO DE DOCUMENTAÇÃO (SWAGGER)
// ==========================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Produtos API",
        Version = "v2.0.0",
        Description = "API REST educacional de produtos com Minimal API em .NET 10 LTS",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    var xmlFile = Path.Combine(AppContext.BaseDirectory, "ProdutosAPI.xml");
    if (File.Exists(xmlFile))
    {
        c.IncludeXmlComments(xmlFile);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT neste campo da seguinte forma: 'Bearer {seu_token_aqui}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ==========================================
// CRIAR APLICAÇÃO
// ==========================================

var app = builder.Build();

// ==========================================
// EXECUTAR MIGRATIONS E SEED
// ==========================================

// Skip DB initialization in test environment — ApiFactory handles seeding
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    FacShopAPI.Catalogo.Infrastructure.Data.DbSeeder.Seed(dbContext);
}

// ==========================================
// CONFIGURAR MIDDLEWARE
// ==========================================

app.UseExceptionHandling();
app.UseCors("AllowAll");

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Produtos API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<IdempotencyMiddleware>();

// ==========================================
// CONFIGURAR ENDPOINTS
// ==========================================

app.MapAuthEndpoints();

var v1 = app.MapGroup("/api/v1");
var catalogo = v1.MapGroup("/catalogo");
catalogo.MapProdutoEndpoints();
catalogo.MapCategoriaEndpoints();
catalogo.MapVarianteEndpoints();
catalogo.MapAtributoEndpoints();
catalogo.MapMidiaEndpoints();

// Health check simples
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .AllowAnonymous();

// ==========================================
// EXECUTAR APLICAÇÃO
// ==========================================

app.Run();

// Required for integration tests via WebApplicationFactory
/// <summary>
/// Entry point marker used by integration tests via WebApplicationFactory.
/// </summary>
public partial class Program { }