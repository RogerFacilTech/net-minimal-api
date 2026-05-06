using FacShopAPI.Pedidos.AddItemPedido;
using FacShopAPI.Pedidos.CancelPedido;
using FacShopAPI.Pedidos.CreatePedido;
using FacShopAPI.Pedidos.GetPedido;
using FacShopAPI.Pedidos.Infrastructure;
using FacShopAPI.Pedidos.ListPedidos;
using FacShopAPI.Pedidos.Repositories;
using FacShopAPI.Shared.Common;
using FacShopAPI.Shared.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
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
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite($"Data Source={testDbPath}"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=../../../data/facshop.db";
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}

// ==========================================
// CONFIGURAÇÃO DE DEPENDENCY INJECTION
// ==========================================
builder.Services.AddEndpointsFromAssembly(typeof(AddItemEndpoint).Assembly);
builder.Services.AddScoped<IPedidoCommandRepository, PedidoCommandRepository>();
builder.Services.AddScoped<IPedidoQueryRepository, PedidoQueryRepository>();

builder.Services.AddScoped<CreatePedidoHandler>();
builder.Services.AddScoped<GetPedidoHandler>();
builder.Services.AddScoped<ListPedidosHandler>();
builder.Services.AddScoped<AddItemHandler>();
builder.Services.AddScoped<CancelPedidoHandler>();

builder.Services.AddValidatorsFromAssemblyContaining<AddItemValidator>();

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

app.MapRegisteredEndpoints();

app.Run();
