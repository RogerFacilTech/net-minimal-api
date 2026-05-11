# Como Criar um Novo Micro-Serviço (Passo a Passo)

## Objetivo

Este guia mostra, de forma pratica, como criar um novo micro-serviço neste repositorio, usando como referencia os servicos de Catalogo e Pedidos.

Foco do documento:

- Subir um Host funcional
- Expor endpoints Minimal API
- Configurar banco de dados com EF Core
- Habilitar Swagger/OpenAPI
- Configurar autenticacao JWT
- Organizar DI, middleware e observabilidade

Nao entra em detalhes de modelagem de entidades de negocio.

## Regra de autenticacao da solution

Padrao atual para novos microservicos:

- Apenas o microservico `Auth` emite JWT.
- Microservicos de dominio (Catalogo, Pedidos e novos servicos) apenas validam JWT.
- Nao criar endpoint de login em servicos de dominio.

Quando criar um novo servico, configure somente validacao JWT (`AddJwtBearer`) e `RequireAuthorization()` nos endpoints protegidos.

---

## 1. Entenda os dois estilos usados no repositorio

Antes de criar um novo servico, escolha o estilo arquitetural:

- Estilo Catalogo (Clean Architecture hibrida):
    - 4 projetos: Domain, Application, Infrastructure, API
    - Endpoints e extensoes dentro do projeto API
    - Endpoints mapeados por grupos no Program.cs
- Estilo Pedidos (Vertical Slice):
    - 3 projetos: Domain, Infrastructure, API
    - Features (slices) como pastas dentro do projeto API
    - Descoberta automatica de endpoints via interface IEndpoint

Para um iniciante, a opcao mais simples para comecar rapido e:

- 1 projeto API (entrada + endpoints)
- 1 projeto Infrastructure (DbContext + migrations + repositorios)

Depois voce adiciona Domain e Application quando o dominio crescer.

---

## 2. Estrutura minima recomendada

Exemplo para um novo contexto chamado Faturamento:

```text
src/Faturamento/
  Faturamento.API/              # entrada da API (Program.cs + endpoints)
  Faturamento.Infrastructure/   # DbContext, migrations e repositorios
```

Opcional (quando precisar separar camadas de dominio):

```text
  Faturamento.Domain/
  Faturamento.Application/
```

---

## 3. Criacao dos projetos (comandos)

A partir da raiz do repositorio:

```bash
dotnet new web -n Faturamento.API -o src/Faturamento/Faturamento.API
dotnet new classlib -n Faturamento.Infrastructure -o src/Faturamento/Faturamento.Infrastructure
```

Adicionar os projetos na solucao:

```bash
dotnet sln FacShop.slnx add src/Faturamento/Faturamento.API/Faturamento.API.csproj
dotnet sln FacShop.slnx add src/Faturamento/Faturamento.Infrastructure/Faturamento.Infrastructure.csproj
```

---

## 4. Referencias entre projetos

No projeto API, adicione referencias para:

- Faturamento.Infrastructure
- Shared.Data (para extensao de migration)
- Shared.Web (se usar autodiscovery de endpoints com IEndpoint)

No projeto Infrastructure, nao ha dependencias internas (apenas NuGet).

### Exemplos de como adicionar uma referência entre projetos

No terminal, navegue até a raiz do repositório e use o comando:

```bash
# Adicionar referência do API para Infrastructure
dotnet add src/Faturamento/Faturamento.API/Faturamento.API.csproj reference src/Faturamento/Faturamento.Infrastructure/Faturamento.Infrastructure.csproj

# Adicionar referência do API para Shared.Data
dotnet add src/Faturamento/Faturamento.API/Faturamento.API.csproj reference src/Shared/Data/Shared.Data.csproj

# Adicionar referência do API para Shared.Web (opcional, para autodiscovery IEndpoint)
dotnet add src/Faturamento/Faturamento.API/Faturamento.API.csproj reference src/Shared/Web/Shared.Web.csproj
```

---

## 5. Pacotes NuGet minimos

No API:

- Swashbuckle.AspNetCore
- Microsoft.AspNetCore.Authentication.JwtBearer
- Serilog
- Serilog.AspNetCore
- Serilog.Sinks.Console
- Serilog.Sinks.File
- FluentValidation
- FluentValidation.DependencyInjectionExtensions

No Infrastructure:

- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.Sqlite
- Microsoft.EntityFrameworkCore.Design (com PrivateAssets=all)
- Dapper (opcional, para queries de leitura)

---

## 6. Configuracao do banco de dados

### 6.1 Criar DbContext

No projeto Faturamento.Infrastructure, crie um DbContext simples.

Exemplo minimo:

```csharp
using Microsoft.EntityFrameworkCore;

namespace FacShopAPI.Faturamento.Infrastructure;

public class FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> options) : DbContext(options)
{
    public DbSet<FaturaEntity> Faturas => Set<FaturaEntity>();
}

public class FaturaEntity
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
```

### 6.2 Registrar DbContext no Program.cs

Padrao usado em Catalogo e Pedidos:

- Em Testing: banco SQLite temporario por teste
- Fora de Testing: connection string de appsettings

```csharp
if (builder.Environment.IsEnvironment("Testing"))
{
    var testDbName = $"TestDb_{Guid.NewGuid():N}.db";
    var testDbPath = Path.Combine(Path.GetTempPath(), testDbName);
    builder.Services.AddDbContext<FaturamentoDbContext>(options =>
        options.UseSqlite($"Data Source={testDbPath}",
            o => o.MigrationsHistoryTable("__EFMigrationsHistory_Faturamento")));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=../../../data/facshop.db";

    builder.Services.AddDbContext<FaturamentoDbContext>(options =>
        options.UseSqlite(connectionString,
            o => o.MigrationsHistoryTable("__EFMigrationsHistory_Faturamento")));
}
```

### 6.3 Criar migration inicial

```bash
dotnet ef migrations add InitialFaturamento --project src/Faturamento/Faturamento.Infrastructure/Faturamento.Infrastructure.csproj --startup-project src/Faturamento/Faturamento.API/Faturamento.API.csproj
```

### 6.4 Aplicar migration na subida do host

Este repositorio ja tem extensao para isso em Shared.Data:

```csharp
if (!app.Environment.IsEnvironment("Testing"))
{
    app.MigrateDatabase<FaturamentoDbContext>();
}
```

Se precisar seed inicial:

```csharp
app.MigrateDatabase<FaturamentoDbContext>(db =>
{
    // seed opcional
});
```

---

## 7. Configurar endpoints

Voce tem dois caminhos.

### 7.1 Caminho A (estilo Catalogo): mapeamento por extensao estatica

Crie uma classe de endpoints no projeto Endpoints:

```csharp
public static class FaturaEndpoints
{
    public static void MapFaturaEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", () => Results.Ok(new { ok = true }))
            .WithName("ListarFaturas")
            .WithTags("Faturamento")
            .AllowAnonymous();

        group.MapPost("/", () => Results.Created("/api/v1/faturamento/faturas/1", new { id = 1 }))
            .WithName("CriarFatura")
            .WithTags("Faturamento")
            .RequireAuthorization();
    }
}
```

No Program.cs:

```csharp
var v1 = app.MapGroup("/api/v1");
var faturamento = v1.MapGroup("/faturamento");
faturamento.MapFaturaEndpoints();
```

### 7.2 Caminho B (estilo Pedidos): autodiscovery com IEndpoint

Se quiser o mesmo padrao de Pedidos:

1. Implemente IEndpoint no endpoint.
2. Registre scan no DI.
3. Chame MapRegisteredEndpoints().

Exemplo endpoint:

```csharp
using FacShopAPI.Shared.Web;

public class CreateFaturaEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/faturamento/faturas", () =>
            Results.Created("/api/v1/faturamento/faturas/1", new { id = 1 }))
            .WithName("CriarFatura")
            .WithTags("Faturamento")
            .RequireAuthorization();
    }
}
```

No Program.cs:

```csharp
builder.Services.AddEndpointsFromAssembly(typeof(CreateFaturaEndpoint).Assembly);
app.MapRegisteredEndpoints();
```

---

## 8. Configurar autenticacao e autorizacao (JWT)

Padrao base usado nos hosts:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var jwtKey = builder.Configuration["Jwt:Key"] ?? "TrocarEstaChaveEmProducao_32bytes_min";

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
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Clientes",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
```

No pipeline:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Regra pratica para endpoints:

- GET: normalmente AllowAnonymous()
- POST/PUT/PATCH/DELETE: RequireAuthorization()

---

## 9. Configurar Swagger/OpenAPI

No Program.cs:

```csharp
using Microsoft.OpenApi.Models;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Faturamento API",
        Version = "v1.0.0",
        Description = "API de Faturamento"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Informe: Bearer {token}",
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
```

No pipeline:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Faturamento API v1");
        c.RoutePrefix = string.Empty;
    });
}
```

---

## 10. Middleware essencial

Ordem recomendada do pipeline (baseado em Catalogo e Pedidos):

1. Tratamento global de excecao (se houver)
2. CORS (se houver)
3. HTTPS redirection (normalmente em producao)
4. Swagger (apenas desenvolvimento)
5. Rate limiter (se habilitado)
6. Authentication
7. Authorization
8. Middlewares de negocio (ex: idempotencia)
9. Mapeamento de endpoints

Exemplo:

```csharp
app.UseExceptionHandling(); // se o projeto tiver extensao
app.UseCors("AllowAll");   // se a API for consumida por front/externo

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .AllowAnonymous();
```

---

## 11. Rate limiting (opcional, mas recomendado)

No Catalogo, ha 3 politicas diferentes por tipo de operacao. Para um novo servico, comece com duas:

- leitura
- escrita

Exemplo de uso no endpoint:

```csharp
group.MapGet("/", Handler)
    .RequireRateLimiting("leitura");

group.MapPost("/", Handler)
    .RequireRateLimiting("escrita");
```

No Program.cs:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("leitura", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
    });

    options.AddSlidingWindowLimiter("escrita", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 6;
        opt.PermitLimit = 20;
    });
});

app.UseRateLimiter();
```

---

## 12. appsettings.json minimo

Exemplo:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Data Source=../../../data/facshop.db"
    },
    "Jwt": {
        "Key": "TrocarEstaChaveEmProducao_32bytes_min",
        "Issuer": "FacShopAPI",
        "Audience": "Clientes"
    },
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning"
        }
    },
    "AllowedHosts": "*"
}
```

---

## 13. Program.cs de referencia (minimo funcional)

```csharp
using System.Text;
using FacShopAPI.Faturamento.Infrastructure;
using FacShopAPI.Shared.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Banco
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=../../../data/facshop.db";

builder.Services.AddDbContext<FaturamentoDbContext>(options =>
    options.UseSqlite(connectionString,
        o => o.MigrationsHistoryTable("__EFMigrationsHistory_Faturamento")));

// Auth
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TrocarEstaChaveEmProducao_32bytes_min";
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
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Clientes",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Faturamento API",
        Version = "v1.0.0"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Faturamento API v1");
        c.RoutePrefix = string.Empty;
    });
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.MigrateDatabase<FaturamentoDbContext>();
}

app.UseAuthentication();
app.UseAuthorization();

var v1 = app.MapGroup("/api/v1");
var faturamento = v1.MapGroup("/faturamento");

faturamento.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.Run();
```

---

## 14. Checklist final para o host funcionar

Antes de subir o servico, confirme:

- Projetos criados e adicionados na solucao
- Referencias entre projetos configuradas
- DbContext registrado no Program.cs
- Migration inicial criada
- app.MigrateDatabase<TContext>() chamado no startup
- Endpoints mapeados (manual ou IEndpoint)
- AddAuthentication/AddAuthorization configurados
- UseAuthentication/UseAuthorization no pipeline
- Swagger registrado e habilitado em Development
- appsettings.json com ConnectionStrings e Jwt
- Endpoint /health respondendo 200

---

## 15. Como validar rapidamente

Executar build:

```bash
dotnet build src/Faturamento/Faturamento.API/Faturamento.API.csproj
```

Executar host:

```bash
dotnet run --project src/Faturamento/Faturamento.API/Faturamento.API.csproj
```

Validacoes esperadas:

- Swagger abre na raiz da URL (RoutePrefix vazio)
- GET /api/v1/faturamento/health retorna 200
- Endpoints de escrita exigem token JWT
- Banco recebe migrations ao iniciar

---

## 16. Referencias no repositorio (para copiar padroes)

- Host do Catalogo: src/Catalogo/Catalogo.API/Program.cs
- Host de Pedidos: src/Pedidos/Pedidos.API/Program.cs
- Registro automatico de endpoints: src/Shared/Web/EndpointExtensions.cs
- Contrato de endpoint em Vertical Slice: src/Shared/Web/IEndpoint.cs
- Extensao para migrations na startup: src/Shared/Data/DbInitializationExtensions.cs
- Exemplo de endpoint com auth e rate limit: src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs
- Exemplo de endpoint Vertical Slice: src/Pedidos/Pedidos.API/CreatePedido/CreatePedidoEndpoint.cs

Com esse roteiro, um desenvolvedor novo no projeto consegue criar um microservico do zero e chegar ate um Host funcional com boas praticas minimas de API.
