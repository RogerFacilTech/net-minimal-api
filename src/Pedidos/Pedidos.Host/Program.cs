using System.Text;
using FacShopAPI.Pedidos.AddItemPedido;
using FacShopAPI.Pedidos.CancelPedido;
using FacShopAPI.Pedidos.CreatePedido;
using FacShopAPI.Pedidos.Data;
using FacShopAPI.Pedidos.GetPedido;
using FacShopAPI.Pedidos.Infrastructure;
using FacShopAPI.Pedidos.ListPedidos;
using FacShopAPI.Pedidos.Repositories;
using FacShopAPI.Shared.Data;
using FacShopAPI.Shared.Http;
using FacShopAPI.Shared.Web;
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
        path: "logs/pedidos-api-.txt",
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
    builder.Services.AddDbContext<PedidosDbContext>(options =>
        options.UseSqlite($"Data Source={testDbPath}",
            o => o.MigrationsHistoryTable("__EFMigrationsHistory_Pedidos")));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=../../../data/facshop.db";
    builder.Services.AddDbContext<PedidosDbContext>(options =>
        options.UseSqlite(connectionString,
            o => o.MigrationsHistoryTable("__EFMigrationsHistory_Pedidos")));
}

// ==========================================
// CONFIGURAÇÃO DE DEPENDENCY INJECTION
// ==========================================

var catalogoApiBaseUrl = builder.Configuration["CatalogoApi:BaseUrl"] ?? "https://localhost:5001";

builder.Services.AddApiClientWithResilience<CatalogoApiClient>(
    clientName: "catalogo-api",
    configureOptions: options =>
    {
        options.BaseUrl = catalogoApiBaseUrl;
        options.AttemptTimeoutSeconds = 5;
        options.TotalRequestTimeoutSeconds = 30;
        options.MaxRetryAttempts = 3;
        options.BaseRetryDelaySeconds = 1;
        options.CircuitSamplingWindowSeconds = 30;
        options.CircuitMinimumThroughput = 5;
        options.CircuitFailureRatio = 0.5;
        options.CircuitBreakDurationSeconds = 15;
    });

builder.Services.AddScoped<ICatalogoApiClient>(sp => sp.GetRequiredService<CatalogoApiClient>());
builder.Services.AddEndpointsFromAssembly(typeof(AddItemEndpoint).Assembly);
builder.Services.AddScoped<IPedidoCommandRepository, PedidoCommandRepository>();
builder.Services.AddScoped<IPedidoQueryRepository, PedidoQueryRepository>();

builder.Services.AddScoped<CreatePedidoHandler>();
builder.Services.AddScoped<GetPedidoHandler>();
builder.Services.AddScoped<ListPedidosHandler>();
builder.Services.AddScoped<AddItemHandler>();
builder.Services.AddScoped<CancelPedidoHandler>();

builder.Services.AddValidatorsFromAssemblyContaining<AddItemValidator>();

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

// SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pedidos API",
        Version = "v1.0.0",
        Description = "API REST educacional de pedidos com Minimal API em .NET 10 LTS"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pedidos API v1");
        c.RoutePrefix = string.Empty;
    });
}

// Skip DB initialization in test environment
if (!app.Environment.IsEnvironment("Testing"))
{
    app.MigrateDatabase<PedidosDbContext>();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapRegisteredEndpoints();

app.Run();
