# Fase 3 — Rate Limiting e Resiliência de Cliente

**Pré-requisito:** Fase 1 e Fase 2 concluídas.  
**Objetivo:** Adicionar rate limiting no servidor (3 políticas), resposta 429 com `Retry-After`, e um projeto `Catalogo.ClientDemo` demonstrando retry + circuit breaker.

---

## Task 1 — Rate Limiting no servidor

### 1.1 — Adicionar NuGet (já incluso no .NET 8+, confirmar)

`AspNetCore.RateLimiting` está incluso no `Microsoft.AspNetCore.App` — não precisa de NuGet extra.

### 1.2 — `src/Catalogo/Catalogo.API/Extensions/RateLimitingExtensions.cs`

```csharp
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ProdutosAPI.Catalogo.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCatalogoRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }
                else
                {
                    context.HttpContext.Response.Headers.RetryAfter = "10";
                }

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"erro":"Too Many Requests","mensagem":"Limite de requisições excedido. Tente novamente em breve."}""",
                    cancellationToken);
            };

            // Política 1: leitura — fixed window, 60 req/min por IP
            options.AddFixedWindowLimiter("leitura", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 60;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Política 2: escrita — sliding window, 20 req/min por IP
            options.AddSlidingWindowLimiter("escrita", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 6; // janelas de 10s
                opt.PermitLimit = 20;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Política 3: criação de produto — token bucket, 5 req/min por IP
            options.AddTokenBucketLimiter("criacao-produto", opt =>
            {
                opt.TokenLimit = 5;
                opt.TokensPerPeriod = 5;
                opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });

        return services;
    }
}
```

### 1.3 — `Program.cs` — registrar middleware e serviço

No bloco de serviços (após `builder.Services.AddCatalogo()`):

```csharp
builder.Services.AddCatalogoRateLimiting();
```

No pipeline de middleware, antes de `app.UseAuthentication()`:

```csharp
app.UseRateLimiter();
```

---

## Task 2 — Aplicar políticas nos endpoints

### 2.1 — `src/Catalogo/Catalogo.API/Endpoints/ProdutoEndpoints.cs`

Adicionar `.RequireRateLimiting()` por verbo:

```csharp
public static void MapProdutoEndpoints(this RouteGroupBuilder catalogoGroup)
{
    var group = catalogoGroup.MapGroup("/produtos")
        .WithTags("Produtos");

    group.MapGet("/", GetAll)
        .WithName("GetAllProdutos")
        .RequireRateLimiting("leitura")
        // ... demais metadados

    group.MapGet("/{id:int}", GetById)
        .WithName("GetProdutoById")
        .RequireRateLimiting("leitura")
        // ...

    group.MapPost("/", Create)
        .WithName("CreateProduto")
        .RequireAuthorization()
        .RequireRateLimiting("criacao-produto")
        // ...

    group.MapPut("/{id:int}", Update)
        .WithName("UpdateProduto")
        .RequireAuthorization()
        .RequireRateLimiting("escrita")
        // ...

    group.MapDelete("/{id:int}", Delete)
        .WithName("DeleteProduto")
        .RequireAuthorization()
        .RequireRateLimiting("escrita")
        // ...
}
```

### 2.2 — `src/Catalogo/Catalogo.API/Endpoints/CategoriaEndpoints.cs`

```csharp
group.MapGet("/", GetAll)      .RequireRateLimiting("leitura");
group.MapGet("/{id:int}", GetById).RequireRateLimiting("leitura");
group.MapPost("/", Create)     .RequireAuthorization().RequireRateLimiting("escrita");
group.MapPut("/{id:int}", Update).RequireAuthorization().RequireRateLimiting("escrita");
group.MapDelete("/{id:int}", Delete).RequireAuthorization().RequireRateLimiting("escrita");
```

### 2.3 — `src/Catalogo/Catalogo.API/Endpoints/VarianteEndpoints.cs`

```csharp
group.MapGet("/", GetAll)              .RequireRateLimiting("leitura");
group.MapGet("/{id:int}", GetById)     .RequireRateLimiting("leitura");
group.MapPost("/", Create)             .RequireAuthorization().RequireRateLimiting("escrita");
group.MapPut("/{id:int}", Update)      .RequireAuthorization().RequireRateLimiting("escrita");
group.MapPatch("/{id:int}/estoque", UpdateEstoque).RequireAuthorization().RequireRateLimiting("escrita");
group.MapDelete("/{id:int}", Delete)   .RequireAuthorization().RequireRateLimiting("escrita");
```

### 2.4 — Atributo e Midia endpoints — mesma pattern de escrita/leitura (omitido por brevidade, seguir o mesmo padrão)

---

## Task 3 — Projeto `Catalogo.ClientDemo`

### 3.1 — Criar csproj: `src/Catalogo/Catalogo.ClientDemo/Catalogo.ClientDemo.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>ProdutosAPI.Catalogo.ClientDemo</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.*" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.*" />
  </ItemGroup>
</Project>
```

Adicionar ao `FacShopAPI.slnx`:

```xml
<Project Path="src/Catalogo/Catalogo.ClientDemo/Catalogo.ClientDemo.csproj" />
```

### 3.2 — `src/Catalogo/Catalogo.ClientDemo/CatalogoHttpClient.cs`

```csharp
using System.Net.Http.Json;

namespace ProdutosAPI.Catalogo.ClientDemo;

public class CatalogoHttpClient(HttpClient http)
{
    public async Task<string?> GetProdutosAsync(CancellationToken ct = default)
    {
        var response = await http.GetAsync("/api/v1/catalogo/produtos", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<HttpResponseMessage> CreateProdutoAsync(object payload, CancellationToken ct = default)
    {
        return await http.PostAsJsonAsync("/api/v1/catalogo/produtos", payload, ct);
    }
}
```

### 3.3 — `src/Catalogo/Catalogo.ClientDemo/ResilienceDemo.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace ProdutosAPI.Catalogo.ClientDemo;

public static class ResilienceDemo
{
    public static IHostApplicationBuilder AddCatalogoClient(
        this IHostApplicationBuilder builder,
        string baseAddress)
    {
        builder.Services
            .AddHttpClient<CatalogoHttpClient>(client =>
            {
                client.BaseAddress = new Uri(baseAddress);
            })
            .AddResilienceHandler("catalogo-pipeline", pipeline =>
            {
                // 1. Timeout por tentativa
                pipeline.AddTimeout(TimeSpan.FromSeconds(5));

                // 2. Retry com exponential backoff — só para erros transientes e 429
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = args => args.Outcome switch
                    {
                        { Exception: HttpRequestException } => PredicateResult.True(),
                        { Result.StatusCode: System.Net.HttpStatusCode.TooManyRequests } => PredicateResult.True(),
                        { Result.StatusCode: System.Net.HttpStatusCode.ServiceUnavailable } => PredicateResult.True(),
                        _ => PredicateResult.False()
                    },
                    OnRetry = args =>
                    {
                        // Respeitar Retry-After do servidor se disponível
                        if (args.Outcome.Result?.Headers.RetryAfter?.Delta is { } retryAfter)
                        {
                            Console.WriteLine($"[Retry] Aguardando {retryAfter.TotalSeconds}s (Retry-After do servidor)...");
                        }
                        else
                        {
                            Console.WriteLine($"[Retry] Tentativa {args.AttemptNumber + 1}...");
                        }
                        return ValueTask.CompletedTask;
                    }
                });

                // 3. Circuit breaker — abre após 5 falhas em 30s
                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    FailureRatio = 0.5,
                    BreakDuration = TimeSpan.FromSeconds(15),
                    OnOpened = args =>
                    {
                        Console.WriteLine($"[CircuitBreaker] Aberto por {args.BreakDuration.TotalSeconds}s");
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = _ =>
                    {
                        Console.WriteLine("[CircuitBreaker] Fechado — serviço recuperado");
                        return ValueTask.CompletedTask;
                    }
                });

                // 4. Timeout global (toda a operação incluindo retries)
                pipeline.AddTimeout(TimeSpan.FromSeconds(30));
            });

        return builder;
    }
}
```

### 3.4 — `src/Catalogo/Catalogo.ClientDemo/Program.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProdutosAPI.Catalogo.ClientDemo;

var builder = Host.CreateApplicationBuilder(args);
var apiBase = args.Length > 0 ? args[0] : "https://localhost:5001";

builder.AddCatalogoClient(apiBase);

var host = builder.Build();
var client = host.Services.GetRequiredService<CatalogoHttpClient>();

Console.WriteLine($"=== Catalogo Client Demo — {apiBase} ===\n");

// Demo 1: leituras em sequência (deve passar todas)
Console.WriteLine("--- Demo 1: Leituras ---");
for (int i = 1; i <= 5; i++)
{
    try
    {
        var result = await client.GetProdutosAsync();
        Console.WriteLine($"  [{i}] OK ({result?.Length} bytes)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [{i}] ERRO: {ex.Message}");
    }
    await Task.Delay(200);
}

// Demo 2: criações em rajada (espera-se 429 após 5)
Console.WriteLine("\n--- Demo 2: Criações em rajada (429 esperado após 5) ---");
var payload = new { nome = "Produto Demo", preco = 99.90, estoque = 10, categoria = "Eletrônicos", descricao = "Demo" };
for (int i = 1; i <= 8; i++)
{
    try
    {
        var response = await client.CreateProdutoAsync(payload);
        Console.WriteLine($"  [{i}] {(int)response.StatusCode} {response.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [{i}] ERRO (após retries): {ex.Message}");
    }
    await Task.Delay(100);
}

Console.WriteLine("\nDemo concluído.");
```

---

## Task 4 — Testes de integração para rate limiting

### 4.1 — `tests/ProdutosAPI.Tests/Integration/RateLimitingTests.cs`

> **Nota:** Rate limiting em testes de integração requer configuração especial. O `WebApplicationFactory` usa um pipeline completo, então `UseRateLimiter()` ativa de verdade. Para testes determinísticos, usar limites muito baixos via `IConfiguration` override na `ApiFactory` de teste.

```csharp
using System.Net;
using FluentAssertions;
using ProdutosAPI.Tests.Helpers;

namespace ProdutosAPI.Tests.Integration;

public class RateLimitingTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_Produtos_QuandoExcedeLimit_Retorna429()
    {
        // Arrange: ApiFactory deve configurar PermitLimit=3 para "leitura" em Testing
        // (ver seção 4.2)

        // Act: disparar 4 requisições
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 4; i++)
        {
            lastResponse = await _client.GetAsync("/api/v1/catalogo/produtos");
        }

        // Assert
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GET_Produtos_QuandoExcedeLimit_RetornaHeaderRetryAfter()
    {
        HttpResponseMessage? rejectedResponse = null;
        for (int i = 0; i < 4; i++)
        {
            var r = await _client.GetAsync("/api/v1/catalogo/produtos");
            if ((int)r.StatusCode == 429)
            {
                rejectedResponse = r;
                break;
            }
        }

        rejectedResponse.Should().NotBeNull();
        rejectedResponse!.Headers.Should().ContainKey("Retry-After");
        var retryAfter = rejectedResponse.Headers.GetValues("Retry-After").First();
        int.Parse(retryAfter).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task POST_Produto_AtingeTetoMaisRapidoQueLeitura()
    {
        // Arrange
        var token = await AuthHelper.ObterTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var payload = new { nome = "P", preco = 1.0, estoque = 1, categoria = "Eletrônicos", descricao = "d" };

        // Act: 2 POSTs (limit=2 para "criacao-produto" em Testing) + 1 extra
        int okCount = 0, tooManyCount = 0;
        for (int i = 0; i < 3; i++)
        {
            var r = await _client.PostAsJsonAsync("/api/v1/catalogo/produtos", payload);
            if (r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.BadRequest) okCount++;
            else if (r.StatusCode == HttpStatusCode.TooManyRequests) tooManyCount++;
        }

        tooManyCount.Should().BeGreaterThan(0);
    }
}
```

### 4.2 — `tests/ProdutosAPI.Tests/Integration/ApiFactory.cs` — override de rate limit para testes

No método `CreateHost` ou via `ConfigureServices`, adicionar um override de rate limit quando `Environment = "Testing"`:

```csharp
// Em ApiFactory.cs, dentro de ConfigureWebHost:
builder.ConfigureServices(services =>
{
    // Remove rate limiter registrado pelo AddCatalogoRateLimiting()
    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(RateLimiterOptions));
    // Como RateLimiterOptions é configurado via IOptions, usar:
    services.Configure<RateLimiterOptions>(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.Headers.RetryAfter = "1";
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(
                """{"erro":"Too Many Requests"}""", ct);
        };

        options.AddFixedWindowLimiter("leitura", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 3; // baixo para testar facilmente
            opt.QueueLimit = 0;
        });

        options.AddSlidingWindowLimiter("escrita", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.SegmentsPerWindow = 6;
            opt.PermitLimit = 3;
            opt.QueueLimit = 0;
        });

        options.AddTokenBucketLimiter("criacao-produto", opt =>
        {
            opt.TokenLimit = 2;
            opt.TokensPerPeriod = 2;
            opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 0;
        });
    });
});
```

> **Alternativa mais limpa:** Extrair os limites para `IConfiguration` (`RateLimiting:Leitura:PermitLimit`) e sobrescrever em `appsettings.Testing.json`. Mais verboso mas evita override de `IOptions` em teste.

---

## Ordem de execução

1. Task 1 — criar `RateLimitingExtensions.cs`
2. Task 1.3 — atualizar `Program.cs`
3. Task 2 — adicionar `RequireRateLimiting()` nos 5 grupos de endpoints
4. Task 3 — criar projeto `Catalogo.ClientDemo` (4 arquivos)
5. Task 4 — criar `RateLimitingTests.cs` e atualizar `ApiFactory.cs`
6. Rodar `dotnet build` e `dotnet test`
7. Commit: `feat: rate limiting com 3 políticas e cliente resiliente`

---

## Checklist de validação

- [ ] `GET /api/v1/catalogo/produtos` retorna 429 após exceder limite de leitura
- [ ] Resposta 429 contém header `Retry-After`
- [ ] `POST /api/v1/catalogo/produtos` atinge limite antes de `GET`
- [ ] `dotnet run --project src/Catalogo/Catalogo.ClientDemo` executa sem erros de compilação
- [ ] `dotnet test` — todos os testes existentes ainda passam
- [ ] Testes de rate limiting passam (podem ser frágeis por timing — isolar com `[Trait("Category", "RateLimit")]` se necessário)
