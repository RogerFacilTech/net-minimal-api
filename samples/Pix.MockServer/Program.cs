using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Pix.MockServer.Application;
using Pix.MockServer.Contracts;
using Pix.MockServer.Infrastructure;
using Pix.MockServer.Infrastructure.InMemory;
using Pix.MockServer.OpenApi;
using Pix.MockServer.Security;

var builder = WebApplication.CreateBuilder(args);
const string mtlsCertPassword = "pix-demo-123";
const string expectedClientSubject = "pix-demo-client";

var mtlsBundle = PixMtlsCertificateStore.Ensure(builder.Environment.ContentRootPath, mtlsCertPassword, expectedClientSubject);

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(5099, listenOptions =>
        {
            listenOptions.UseHttps(httpsOptions =>
            {
                httpsOptions.ServerCertificate = X509CertificateLoader.LoadPkcs12FromFile(
                    mtlsBundle.ServerCertificatePath,
                    mtlsBundle.CertificatePassword,
                    X509KeyStorageFlags.Exportable);
                httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                httpsOptions.CheckCertificateRevocation = false;
                httpsOptions.ClientCertificateValidation = (certificate, _, _) =>
                    PixMtlsCertificateStore.ValidateClientCertificate(
                        certificate,
                        mtlsBundle.CaCertificatePath,
                        mtlsBundle.ExpectedClientSubject);
            });
        });
    });
}

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PIX Mock Processing Server",
        Version = "v1",
        Description = "Servidor didático auto-contido para demonstrar chamadas API PIX com JSON complexo."
    });
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(mtlsBundle);
builder.Services.AddSingleton<ICobrancaRepository, InMemoryCobrancaRepository>();
builder.Services.AddSingleton<IDevolucaoRepository, InMemoryDevolucaoRepository>();
builder.Services.AddSingleton<IIdempotencyRepository, InMemoryIdempotencyRepository>();
builder.Services.AddSingleton<ICobrancaService, CobrancaService>();
builder.Services.AddSingleton<IDevolucaoService, DevolucaoService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IMockSecurityValidator, MockSecurityValidator>();

builder.Services.AddDbContext<PixDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "pix-mock-server" }))
    .WithName("PixHealth")
    .AllowAnonymous();

app.MapPost("/oauth/token", (OAuthTokenRequest request, ITokenService tokenService, HttpContext context) =>
{
    var correlationId = GetOrCreateCorrelationId(context);

    if (!string.Equals(request.GrantType, "client_credentials", StringComparison.Ordinal))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["grant_type"] = ["Grant type suportado: client_credentials"]
        }, extensions: BuildExtensions(context, correlationId));
    }

    if (!string.Equals(request.ClientId, "pix-demo-client", StringComparison.Ordinal) ||
        !string.Equals(request.ClientSecret, "pix-demo-secret", StringComparison.Ordinal))
    {
        return Results.Problem(
            title: "Credenciais inválidas",
            detail: "client_id/client_secret inválidos.",
            statusCode: StatusCodes.Status401Unauthorized,
            extensions: BuildExtensions(context, correlationId));
    }

    var token = tokenService.IssueToken(request.ClientId, SwaggerExamples.Scope, TimeSpan.FromMinutes(30));
    return Results.Ok(new OAuthTokenResponse(token, "Bearer", 1800, SwaggerExamples.Scope));
})
.WithName("OAuthToken")
.WithSummary("Emite token OAuth2 mock (client credentials).")
.AllowAnonymous();

var pixGroup = app.MapGroup("/pix/v1");

pixGroup.MapPost("/cobrancas", (
    CriarCobrancaRequest request,
    HttpContext context,
    IMockSecurityValidator securityValidator,
    ICobrancaService cobrancaService) =>
{
    if (!securityValidator.TryValidate(context, out var securityFailure, out var correlationId))
    {
        return securityFailure!;
    }

    if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey) || string.IsNullOrWhiteSpace(idempotencyKey))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Idempotency-Key"] = ["Header Idempotency-Key é obrigatório."]
        }, extensions: BuildExtensions(context, correlationId));
    }

    var validation = PixValidation.ValidateCobranca(request);
    if (validation.Count > 0)
    {
        return Results.ValidationProblem(validation, extensions: BuildExtensions(context, correlationId));
    }

    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var outcome = cobrancaService.Create(request, idempotencyKey.ToString(), correlationId, baseUrl);
    if (outcome.IsConflict)
    {
        return Results.Conflict(new
        {
            title = "Conflito de idempotência",
            detail = "A mesma chave de idempotência foi usada com payload diferente.",
            traceId = context.TraceIdentifier,
            correlationId
        });
    }

    if (outcome.IsIdempotentReplay)
    {
        return Results.Ok(outcome.Response);
    }

    return Results.Created($"/pix/v1/cobrancas/{outcome.Response.Txid}", outcome.Response);
})
.WithName("CriarCobrancaPix")
.WithSummary("Cria cobrança PIX imediata.")
.Produces<CobrancaResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status409Conflict);

pixGroup.MapGet("/cobrancas/{txid}", (
    string txid,
    HttpContext context,
    IMockSecurityValidator securityValidator,
    ICobrancaService cobrancaService) =>
{
    if (!securityValidator.TryValidate(context, out var securityFailure, out _))
    {
        return securityFailure!;
    }

    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var response = cobrancaService.Get(txid, baseUrl);
    return response is null ? Results.NotFound() : Results.Ok(response);
})
.WithName("ObterCobrancaPix")
.WithSummary("Consulta cobrança PIX por txid.");

pixGroup.MapPost("/cobrancas/{txid}/simular-liquidacao", (
    string txid,
    HttpContext context,
    IMockSecurityValidator securityValidator,
    ICobrancaService cobrancaService) =>
{
    if (!securityValidator.TryValidate(context, out var securityFailure, out _))
    {
        return securityFailure!;
    }

    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var response = cobrancaService.SimularLiquidacao(txid, baseUrl);
    return response is null ? Results.NotFound() : Results.Ok(response);
})
.WithName("SimularLiquidacaoCobranca")
.WithSummary("Simula liquidação de cobrança PIX.");

pixGroup.MapPost("/devolucoes", (
    CriarDevolucaoRequest request,
    HttpContext context,
    IMockSecurityValidator securityValidator,
    ICobrancaService cobrancaService,
    IDevolucaoService devolucaoService) =>
{
    if (!securityValidator.TryValidate(context, out var securityFailure, out var correlationId))
    {
        return securityFailure!;
    }

    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var cobranca = cobrancaService.Get(request.Txid, baseUrl);
    if (cobranca is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["txid"] = ["Cobrança informada não existe."]
        }, extensions: BuildExtensions(context, correlationId));
    }

    var validation = PixValidation.ValidateDevolucao(request, cobranca.ValorOriginal);
    if (validation.Count > 0)
    {
        return Results.ValidationProblem(validation, extensions: BuildExtensions(context, correlationId));
    }

    var devolucao = devolucaoService.Create(request, correlationId);
    if (devolucao is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["status"] = ["A devolução só pode ser criada após a cobrança estar CONCLUIDA."]
        }, extensions: BuildExtensions(context, correlationId));
    }

    return Results.Created($"/pix/v1/devolucoes/{devolucao.DevolucaoId}", devolucao);
})
.WithName("CriarDevolucaoPix")
.WithSummary("Cria devolução de transação PIX.")
.Produces<DevolucaoResponse>(StatusCodes.Status201Created);

pixGroup.MapGet("/devolucoes/{devolucaoId}", (
    string devolucaoId,
    HttpContext context,
    IMockSecurityValidator securityValidator,
    IDevolucaoService devolucaoService) =>
{
    if (!securityValidator.TryValidate(context, out var securityFailure, out _))
    {
        return securityFailure!;
    }

    var response = devolucaoService.Get(devolucaoId);
    return response is null ? Results.NotFound() : Results.Ok(response);
})
.WithName("ObterDevolucaoPix")
.WithSummary("Consulta devolução PIX por identificador.");

app.Run();

static string GetOrCreateCorrelationId(HttpContext context)
{
    var header = context.Request.Headers["X-Correlation-Id"].ToString();
    if (!string.IsNullOrWhiteSpace(header))
    {
        return header;
    }

    var value = Guid.NewGuid().ToString("N");
    context.Request.Headers["X-Correlation-Id"] = value;
    return value;
}

static Dictionary<string, object?> BuildExtensions(HttpContext context, string correlationId)
    => new()
    {
        ["traceId"] = context.TraceIdentifier,
        ["correlationId"] = correlationId
    };

public partial class Program;
