# Melhores Práticas de Minimal API em .NET - Guia de Implementação

## Introdução

Este documento explica como as melhores práticas de API REST apresentadas em `MELHORES-PRATICAS-API.md` foram implementadas no projeto de exemplo usando .NET 10 e Minimal API.

## Versão do .NET

- **.NET 10.0** - Versão LTS mais moderna com suporte estendido
- **Minimal API** - Abordagem simplificada para criar APIs sem controllers

### Por que Minimal API?

A Minimal API é ideal para:

- ✅ APIs simples e diretas
- ✅ Microserviços
- ✅ APIs com poucos endpoints
- ✅ Prototipagem rápida
- ✅ Menor overhead de framework

---

## Estrutura do Projeto

```
net-minimal-api/
├── src/
│   ├── Catalogo/                           # Bounded Context 1 — Clean Architecture híbrida
│   │   ├── Catalogo.Domain/                # Entidades, value objects, interfaces de repositório
│   │   │   ├── Produto.cs                  # Aggregate com Produto.Criar() → Result<Produto>
│   │   │   ├── Categoria.cs               # Slug gerado, hierarquia pai/filho
│   │   │   ├── Variante.cs                # SKU value object
│   │   │   ├── Atributo.cs / Midia.cs     # CRUD simples (anêmico)
│   │   │   └── Common/                    # PrecoProduto, EstoqueProduto, DomainResult
│   │   ├── Catalogo.Application/
│   │   │   ├── Services/                  # Orquestração — ProdutoService, etc.
│   │   │   ├── DTOs/                      # Produto/, Categoria/, Variante/, etc.
│   │   │   ├── Validators/                # FluentValidation por recurso
│   │   │   ├── Repositories/              # Interfaces Query/Command (CQRS leve)
│   │   │   └── Mappings/                  # AutoMapper profiles
│   │   ├── Catalogo.Infrastructure/       # Repositórios EF Core, DbSeeder
│   │   ├── Catalogo.API/
│   │   │   ├── Endpoints/                 # Um arquivo por recurso
│   │   │   │   ├── Produtos/ProdutoEndpoints.cs
│   │   │   │   ├── Categorias/CategoriaEndpoints.cs
│   │   │   │   ├── Variantes/VarianteEndpoints.cs
│   │   │   │   ├── Atributos/AtributoEndpoints.cs
│   │   │   │   ├── Midias/MidiaEndpoints.cs
│   │   │   │   └── Auth/AuthEndpoints.cs
│   │   │   └── Extensions/
│   │   │       └── RateLimitingExtensions.cs  # 3 políticas
│   │   └── Catalogo.ClientDemo/           # Console app — resiliência Polly v8
│   │
│   ├── Pedidos/                           # Bounded Context 2 — Vertical Slice + Domínio Rico
│   │   ├── Domain/                        # Pedido aggregate, Result<T>
│   │   └── Features/                      # CreatePedido/, GetPedido/, etc.
│   │
│   ├── Pix/                               # Bounded Context 3 — Mock + Cliente HTTP
│   │   ├── Pix.MockServer/                # Simula BCB Pix (OAuth2 + mTLS)
│   │   └── Pix.ClientDemo/                # HttpClient tipado com resiliência
│   │
│   └── Shared/
│       ├── Common/                        # IEndpoint, Result<T>, EndpointExtensions
│       ├── Data/                          # AppDbContext + Migrations + DbSeeder
│       └── Middleware/                    # ExceptionHandling, Idempotency
│
└── tests/
    ├── ProdutosAPI.Tests/                 # 143 testes — Catálogo e Pedidos
    └── Pix.MockServer.Tests/              # 7 testes — integração PIX
```

---

## Implementação das Melhores Práticas

### Boas práticas de cliente HTTP (integrações externas)

Além dos endpoints internos, o projeto também demonstra consumo de API externa simulada:

- `HttpClientFactory` com cliente tipado (`PixProcessingClient`);
- `AddStandardResilienceHandler` para retry e timeout;
- `DelegatingHandler` para `X-Correlation-Id`, `Idempotency-Key` e logging;
- `AuthTokenProvider` com cache de token OAuth2 mock.

Referências:

- [samples/Pix/Pix.ClientDemo/Program.cs](../samples/Pix/Pix.ClientDemo/Program.cs)
- [samples/Pix/Pix.ClientDemo/Client/PixProcessingClient.cs](../samples/Pix/Pix.ClientDemo/Client/PixProcessingClient.cs)
- [samples/Pix/Pix.ClientDemo/Client/AuthTokenProvider.cs](../samples/Pix/Pix.ClientDemo/Client/AuthTokenProvider.cs)

### 1. RESTful Design

#### ✅ Identificação de Recursos

**Referência**: MELHORES-PRATICAS-API.md - Seção "RESTful Design"

Os endpoints seguem a convenção REST com recursos bem definidos:

```csharp
// src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs
// prefixo definido no MapGroup do Catálogo: /api/v1/catalogo/produtos

// Recursos identificados por URI
GET    /api/v1/catalogo/produtos              → Listar produtos
GET    /api/v1/catalogo/produtos/{id}         → Obter específico
POST   /api/v1/catalogo/produtos              → Criar novo
PUT    /api/v1/catalogo/produtos/{id}         → Atualizar completo
PATCH  /api/v1/catalogo/produtos/{id}         → Atualizar parcial
DELETE /api/v1/catalogo/produtos/{id}         → Deletar
```

**Implementação**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L15-L20)

#### ✅ Operações Padrão HTTP

Cada endpoint usa o verbo HTTP correto:

```csharp
// POST - Criar (custo alto → política mais restritiva)
group.MapPost("/", CriarProduto)
    .WithName("CriarProduto")
    .Produces<ProdutoResponse>(StatusCodes.Status201Created)
    .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
    .RequireAuthorization()
    .RequireRateLimiting("criacao-produto");    // TokenBucket, 5 req/min

// GET - Recuperar (idempotente, anônimo, rate limit suave)
group.MapGet("/{id}", ObterProduto)
    .WithName("ObterProduto")
    .Produces<ProdutoResponse>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
    .AllowAnonymous()
    .RequireRateLimiting("leitura");           // FixedWindow, 60 req/min

// PUT - Substituir completamente
group.MapPut("/{id}", AtualizarCompletoProduto)
    .Produces<ProdutoResponse>(StatusCodes.Status200OK)
    .RequireAuthorization()
    .RequireRateLimiting("escrita");           // SlidingWindow, 20 req/min

// PATCH - Atualizar parcialmente
group.MapPatch("/{id}", AtualizarParcialProduto)
    .Produces<ProdutoResponse>(StatusCodes.Status200OK)
    .RequireAuthorization()
    .RequireRateLimiting("escrita");

// DELETE - Soft delete (Ativo = false, produto vira 404)
group.MapDelete("/{id}", DeletarProduto)
    .Produces(StatusCodes.Status204NoContent)
    .RequireAuthorization()
    .RequireRateLimiting("escrita");
```

**Implementação**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L29-L60)

#### ✅ Representação Padronizada

**Referência**: MELHORES-PRATICAS-API.md - Seção "Representação de Recursos"

Respostas padronizadas em JSON usando DTOs:

```csharp
// src/Catalogo/Catalogo.Application/DTOs/ProdutoDTO.cs
public class ProdutoResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public int Estoque { get; set; }
    public bool Ativo { get; set; }
    public string ContatoEmail { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}
```

**Implementação**: [src/Catalogo/Catalogo.Application/DTOs/ProdutoDTO.cs](../src/Catalogo/Catalogo.Application/DTOs/ProdutoDTO.cs#L25)

#### ✅ Statelessness

Cada requisição deve conter todas as informações necessárias:

```csharp
// Não mantém estado de sessão
// Autenticação futura: JWT token em header Authorization

private static async Task<IResult> ListarProdutos(
    IProdutoService produtoService,
    int page = 1,
    int pageSize = 20,
    string? categoria = null,
    string? search = null)
{
    // Toda informação está na requisição
    var resultado = await produtoService.ListarProdutosAsync(
        page, pageSize, categoria, search);

    return Results.Ok(resultado);
}
```

**Implementação**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L70-L80)

---

### 2. Design de Endpoints

#### ✅ Nomenclatura de URLs

**Referência**: MELHORES-PRATICAS-API.md - Seção "Nomenclatura de URLs"

```csharp
// ✅ CORRETO: Nomes em plural
GET /api/v1/catalogo/produtos

// ✅ CORRETO: Minúsculas
GET /api/v1/catalogo/produtos/123

// ✅ CORRETO: Hífens para separar palavras
GET /api/v1/catalogo/produtos?status=produto-ativo

// ❌ EVITAR: Verbos nas URLs
// GET /api/v1/obter-produtos
// GET /api/v1/deletar-produto/123
```

**Implementação**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L14-L15)

#### ✅ Paginação

**Referência**: MELHORES-PRATICAS-API.md - Seção "Paginação"

```csharp
// Requisição com paginação
GET /api/v1/catalogo/produtos?page=1&pageSize=20&sortBy=nome

// Resposta paginada
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 150,
    "totalPages": 8
  }
}
```

**Implementação da resposta**: [src/Catalogo/Catalogo.Application/DTOs/ProdutoDTO.cs](../src/Catalogo/Catalogo.Application/DTOs/ProdutoDTO.cs#L46-L57)

**Implementação do endpoint**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L70-L86)

**Implementação do serviço**:

```csharp
// src/Catalogo/Catalogo.Application/Services/ProdutoService.cs
public async Task<PaginatedResponse<ProdutoResponse>> ListarProdutosAsync(
    int page, int pageSize, string? categoria = null, string? search = null)
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20; // Máximo 100

    var query = _context.Produtos.Where(p => p.Ativo).AsQueryable();

    var totalItems = await query.CountAsync();
    var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

    var produtos = await query
        .OrderByDescending(p => p.DataCriacao)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PaginatedResponse<ProdutoResponse>
    {
        Data = _mapper.Map<List<ProdutoResponse>>(produtos),
        Pagination = new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        }
    };
}
```

**Implementação**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L32-L75)

#### ✅ Filtros e Busca

**Referência**: MELHORES-PRATICAS-API.md - Seção "Filtros e Busca"

```csharp
// Suporte a filtros e busca na mesma requisição
GET /api/v1/catalogo/produtos?categoria=eletrônicos&search=notebook

// No serviço:
if (!string.IsNullOrEmpty(categoria))
{
    query = query.Where(p => p.Categoria == categoria);
}

if (!string.IsNullOrEmpty(search))
{
    query = query.Where(p => p.Nome.Contains(search) ||
                             p.Descricao.Contains(search));
}
```

**Implementação**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L48-L55)

---

### 3. Versionamento

**Referência**: MELHORES-PRATICAS-API.md - Seção "Versionamento"

#### ✅ URL Path Versionamento (Recomendado)

```csharp
// Program.cs
// prefixo definido no MapGroup do Catálogo: /api/v1/catalogo/produtos

// Endpoints começam com /api/v1/
// Fácil evoluir para /api/v2/ no futuro
```

**Implementação**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L14)

#### ✅ Versionamento Semântico do Projeto

```xml
<!-- ProdutosAPI.csproj -->
<Version>1.0.0</Version>
<Description>API REST de Produtos com Minimal API e .NET 10</Description>
```

**Implementação**: [ProdutosAPI.csproj](ProdutosAPI.csproj#L8-L9)

---

### 4. Segurança

#### ✅ Validação de Inputs

**Referência**: MELHORES-PRATICAS-API.md - Seção "Segurança - Validação"

Usando **FluentValidation** para validações robustas:

```csharp
// src/Catalogo/Catalogo.Application/Validators/ProdutoValidator.cs
public class CriarProdutoValidator : AbstractValidator<CriarProdutoRequest>
{
    public CriarProdutoValidator()
    {
        RuleFor(p => p.Nome)
            .NotEmpty()
            .WithMessage("Nome é obrigatório")
            .MinimumLength(3)
            .WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(100)
            .WithMessage("Nome não pode exceder 100 caracteres");

        RuleFor(p => p.Preco)
            .GreaterThan(0)
            .WithMessage("Preço deve ser maior que zero");

        RuleFor(p => p.ContatoEmail)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Email de contato inválido");
    }
}
```

**Implementação do validador**: [src/Catalogo/Catalogo.Application/Validators/ProdutoValidator.cs](../src/Catalogo/Catalogo.Application/Validators/ProdutoValidator.cs)

**Uso no endpoint**:

```csharp
// src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs
private static async Task<IResult> CriarProduto(
    CriarProdutoRequest request,
    IValidator<CriarProdutoRequest> validator,
    ...)
{
    var resultado = await validator.ValidateAsync(request);
    if (!resultado.IsValid)
    {
        throw new ValidationException(resultado.Errors);
    }

    var produto = await produtoService.CriarProdutoAsync(request);
    return Results.Created($"/api/v1/catalogo/produtos/{produto.Id}", produto);
}
```

**Implementação**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L125-L145)

#### ✅ Proteção contra SQL Injection

**Referência**: MELHORES-PRATICAS-API.md - Seção "Segurança - SQL Injection"

Usando **Entity Framework Core** (ORM) ao invés de SQL raw:

```csharp
// ✅ SEGURO: Usando EF Core com queries LINQ
var produtos = await _context.Produtos
    .Where(p => p.Nome.Contains(search)) // Parametrizado automaticamente
    .ToListAsync();

// ❌ NÃO FAZER: Raw SQL sem parametrização
// var produtos = _context.Produtos.FromSqlRaw(
//     $"SELECT * FROM Produtos WHERE Nome LIKE '%{search}%'");
```

**Implementação**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L48)

#### ✅ CORS Configurado

**Referência**: MELHORES-PRATICAS-API.md - Seção "Segurança - CORS"

```csharp
// Program.cs
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

// Use CORS
app.UseCors("AllowAll");
```

**Implementação**: [Program.cs](Program.cs#L42-L51)

---

### 5. Validação de Dados

**Referência**: MELHORES-PRATICAS-API.md - Seção "Validação de Dados"

#### ✅ Input Validation

Campos validados conforme business rules:

```csharp
// src/Catalogo/Catalogo.Domain/Produto.cs
public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;     // Min 3 caracteres
    public decimal Preco { get; set; }                   // Deve ser > 0
    public int Estoque { get; set; }                     // Não pode ser negativo
    public string ContatoEmail { get; set; } = string.Empty; // Email válido
    public bool Ativo { get; set; } = true;              // Status soft delete
}
```

**Implementação**: [src/Catalogo/Catalogo.Domain/Produto.cs](../src/Catalogo/Catalogo.Domain/Produto.cs)

#### ✅ Mensagens de Erro de Validação

```csharp
// Resposta de validação
{
  "errors": {
    "nome": ["Campo obrigatório", "Mínimo 3 caracteres"],
    "preco": ["Deve ser maior que 0"],
    "email": ["Email inválido"]
  }
}
```

**Implementação**: [src/Shared/Middleware/ExceptionHandlingMiddleware.cs](../src/Shared/Middleware/ExceptionHandlingMiddleware.cs#L47-L60)

#### ✅ Sanitização

AutoMapper e FluentValidation garantem sanitização:

```csharp
// AutoMapper mapeia e converte tipos
CreateMap<CriarProdutoRequest, Produto>();

// FluentValidation valida formato
RuleFor(p => p.ContatoEmail)
    .EmailAddress()
    .WithMessage("Email de contato inválido");
```

**Implementação**: [src/Catalogo/Catalogo.Application/Mappings/ProdutoMappingProfile.cs](../src/Catalogo/Catalogo.Application/Mappings/ProdutoMappingProfile.cs)

---

### 6. Tratamento de Erros

**Referência**: MELHORES-PRATICAS-API.md - Seção "Tratamento de Erros"

#### ✅ HTTP Status Codes Corretos

```csharp
// GET - 200 OK
Results.Ok(produto)

// POST - 201 Created
Results.Created($"/api/v1/catalogo/produtos/{produto.Id}", produto)

// DELETE - 204 No Content
Results.NoContent()

// 4xx Erros do Cliente
Results.NotFound(...)           // 404
Results.BadRequest(...)         // 400
Results.UnprocessableEntity(...) // 422

// 5xx Erro do Servidor
// Middleware captura e retorna 500
```

**Implementação**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L88-L180)

#### ✅ Respostas de Erro Padronizadas

**Referência**: MELHORES-PRATICAS-API.md - Seção "Resposta de Erro Padronizada"

```csharp
// src/Catalogo/Catalogo.Application/DTOs/ProdutoDTO.cs
public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public Dictionary<string, List<string>> Errors { get; set; } = new();
}
```

**Exemplo de resposta de erro**:

```json
{
    "type": "https://example.com/errors/validation-error",
    "title": "Validation Failed",
    "status": 422,
    "detail": "One or more validation errors occurred.",
    "instance": "/api/v1/catalogo/produtos",
    "errors": {
        "nome": ["Campo obrigatório"],
        "preco": ["Deve ser maior que 0"]
    }
}
```

**Implementação**: [src/Shared/Middleware/ExceptionHandlingMiddleware.cs](../src/Shared/Middleware/ExceptionHandlingMiddleware.cs#L35-L75)

#### ✅ Middleware Global de Tratamento de Erros

```csharp
// src/Shared/Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção não tratada");
            await HandleExceptionAsync(context, ex);
        }
    }
}

// Program.cs
app.UseExceptionHandling();
```

**Implementação**: [src/Shared/Middleware/ExceptionHandlingMiddleware.cs](../src/Shared/Middleware/ExceptionHandlingMiddleware.cs)

---

### 7. Documentação

**Referência**: MELHORES-PRATICAS-API.md - Seção "Documentação"

#### ✅ OpenAPI/Swagger

```csharp
// Program.cs
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Produtos API",
        Version = "v1.0.0",
        Description = "API REST de produtos com Minimal API em .NET 10"
    });
});

// Use Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

**Implementação**: [Program.cs](Program.cs#L80-L100)

#### ✅ Endpoints com Descrição

```csharp
// src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs
group.MapGet("/", ListarProdutos)
    .WithName("ListarProdutos")
    .WithDescription("Lista todos os produtos com paginação")
    .WithSummary("Listar produtos")
    .Produces<PaginatedResponse<ProdutoResponse>>(StatusCodes.Status200OK)
    .AllowAnonymous();
```

**Acesso**: http://localhost:5001 (Swagger UI)

**Implementação**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L22-L27)

#### ✅ XML Comments

```csharp
// src/Catalogo/Catalogo.Domain/Produto.cs
/// <summary>
/// Entidade Produto
/// Referência: MELHORES-PRATICAS-API.md - Seção "Design de Endpoints"
/// Representa um produto no sistema
/// </summary>
public class Produto
{
    /// <summary>
    /// Identificador único do produto (PK)
    /// </summary>
    public int Id { get; set; }
}
```

**Implementação**: [src/Catalogo/Catalogo.Domain/Produto.cs](../src/Catalogo/Catalogo.Domain/Produto.cs#L1)

---

### 8. Performance

**Referência**: MELHORES-PRATICAS-API.md - Seção "Performance"

#### ✅ Paginação Obrigatória

```csharp
// Máximo 100 itens por página
if (pageSize < 1 || pageSize > 100) pageSize = 20;

// Padrão: 20 itens
int pageSize = 20
```

**Implementação**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L41-L42)

#### ✅ Async/Await

```csharp
// Todas as operações I/O são assíncronas
public async Task<PaginatedResponse<ProdutoResponse>> ListarProdutosAsync(...)
{
    var totalItems = await query.CountAsync();
    var produtos = await query.ToListAsync();
    // ...
}
```

**Implementação**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L32)

#### ✅ Índices de Banco de Dados

```csharp
// src/Shared/Data/AppDbContext.cs
entity.HasIndex(p => p.Ativo)
    .HasName("idx_produto_ativo");

entity.HasIndex(p => p.Categoria)
    .HasName("idx_produto_categoria");
```

**Implementação**: [src/Shared/Data/AppDbContext.cs](../src/Shared/Data/AppDbContext.cs#L26-L30)

---

### 9. Logging e Monitoramento

**Referência**: MELHORES-PRATICAS-API.md - Seção "Logging e Monitoramento"

#### ✅ Structured Logging com Serilog

```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/api-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        fileSizeLimitBytes: 10_000_000)
    .WriteTo.File(
        new JsonFormatter(),
        path: "logs/api-.json",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

**Implementação**: [Program.cs](Program.cs#L17-L33)

#### ✅ Logging em Serviços

```csharp
// src/Catalogo/Catalogo.Application/Services/ProdutoService.cs
_logger.LogInformation("Listando produtos - Page: {Page}, PageSize: {PageSize}",
    page, pageSize);

_logger.LogWarning("Produto com ID {ProductId} não encontrado", id);

_logger.LogError(ex, "Erro ao listar produtos");
```

**Implementação**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L34)

#### ✅ Correlação de Requisições

```csharp
// Middleware captura request ID
var requestId = context.TraceIdentifier;

// Disponível em logs
_logger.LogInformation("Requisição: {RequestId}", requestId);
```

---

### 10. Testes

**Referência**: MELHORES-PRATICAS-API.md - Seção "Testes"

Para implementar testes, crie um projeto de teste:

```bash
# Criar projeto de teste
dotnet new xunit --name ProdutosAPI.Tests

# Adicionar referências
dotnet add package Moq
dotnet add package FluentAssertions
```

#### ✅ Exemplo de Teste Unitário

```csharp
// tests/ProdutosAPI.Tests/Services/ProdutoServiceTests.cs
[Fact]
public async Task ListarProdutos_DeveRetornarPaginado()
{
    // Arrange
    var mockContext = new Mock<AppDbContext>();
    var mockLogger = new Mock<ILogger<ProdutoService>>();
    var mockMapper = new Mock<IMapper>();

    var service = new ProdutoService(mockContext.Object, mockMapper.Object, mockLogger.Object);

    // Act
    var resultado = await service.ListarProdutosAsync(1, 20);

    // Assert
    resultado.Pagination.Page.Should().Be(1);
    resultado.Pagination.PageSize.Should().Be(20);
}
```

---

## Como Executar o Projeto

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQLite é gerenciado automaticamente pelo EF Core

### Trilha Catálogo (API principal)

```bash
# 1. Restaurar e executar
dotnet restore
dotnet run --project src/Catalogo/Catalogo.API

# 2. Swagger UI
open http://localhost:5001/swagger

# 3. Autenticar (JWT)
TOKEN=$(curl -s -X POST http://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","senha":"senha123"}' | jq -r .token)

# 4. Listar produtos (anônimo)
curl "http://localhost:5001/api/v1/catalogo/produtos?page=1&pageSize=10"

# 5. Criar produto (requer JWT)
curl -X POST "http://localhost:5001/api/v1/catalogo/produtos" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Notebook Dell",
    "descricao": "Notebook de alto desempenho, 16GB RAM",
    "preco": 3500.00,
    "categoria": "Eletrônicos",
    "estoque": 5,
    "contatoEmail": "vendas@dell.com"
  }'

# 6. Soft delete (produto vira 404 após deleção)
curl -X DELETE "http://localhost:5001/api/v1/catalogo/produtos/1" \
  -H "Authorization: Bearer $TOKEN"

# 7. GET após soft delete → 404
curl "http://localhost:5001/api/v1/catalogo/produtos/1"

# 8. Observar rate limiting (após 5 POSTs rápidos na política criacao-produto → 429)
for i in {1..6}; do
  curl -s -o /dev/null -w "Request $i: %{http_code}\n" \
    -X POST "http://localhost:5001/api/v1/catalogo/produtos" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"nome":"Produto '$i'","descricao":"Desc produto '$i'","preco":10,"categoria":"Outros","estoque":1,"contatoEmail":"t@t.com"}'
done
# Primeiras 5: 201 Created; 6ª: 429 Too Many Requests + header Retry-After
```

### Trilha PIX (integração externa)

```bash
# Terminal 1 — servidor mock
dotnet run --project samples/Pix/Pix.MockServer/Pix.MockServer.csproj

# Terminal 2 — cliente didático
dotnet run --project samples/Pix/Pix.ClientDemo/Pix.ClientDemo.csproj
```

### Rodar testes

```bash
dotnet test FacShopAPI.slnx -v minimal            # todos os 150 testes
dotnet test tests/ProdutosAPI.Tests/ \
  --filter "FullyQualifiedName~RateLimitingTests"  # só rate limiting
```

---

## Estrutura de Pastas (Catálogo)

```
src/Catalogo/
├── Catalogo.Domain/
│   ├── Produto.cs                  ← Aggregate rico: Produto.Criar() → Result<Produto>
│   ├── Categoria.cs               ← Hierarquia + slug auto-gerado
│   ├── Variante.cs                ← SKU value object (sealed record)
│   ├── Atributo.cs / Midia.cs     ← CRUD simples (anêmico, sem invariantes)
│   └── Common/
│       ├── PrecoProduto.cs        ← Value object: preço não negativo
│       ├── EstoqueProduto.cs      ← Value object: estoque não negativo
│       └── DomainResult.cs        ← Result<T> do domínio
│
├── Catalogo.Application/
│   ├── Services/
│   │   ├── IProdutoService.cs     ← Interface do serviço
│   │   └── ProdutoService.cs      ← Orquestração + logging + paginação
│   ├── DTOs/
│   │   └── Produto/
│   │       └── ProdutoDTO.cs      ← CriarProdutoRequest, AtualizarProdutoRequest, ProdutoResponse
│   ├── Validators/
│   │   └── ProdutoValidator.cs    ← CriarProdutoValidator, AtualizarProdutoValidator
│   ├── Repositories/
│   │   ├── IProdutoQueryRepository.cs   ← Retorna DTOs diretamente (leitura)
│   │   └── IProdutoCommandRepository.cs ← Opera sobre entidades (escrita)
│   └── Mappings/
│       └── ProdutoMappingProfile.cs
│
├── Catalogo.Infrastructure/
│   ├── Repositories/              ← Implementações EF Core das interfaces
│   └── Data/DbSeeder.cs           ← 8 produtos e 5 categorias (IDs 1-8 / 1-5 reservados)
│
└── Catalogo.API/
    ├── Endpoints/
    │   └── Produtos/ProdutoEndpoints.cs   ← 6 rotas com RequireRateLimiting
    └── Extensions/
        └── RateLimitingExtensions.cs      ← leitura / escrita / criacao-produto
```

---

## Referências Cruzadas

| Aspecto                       | Guia teórico                                                   | Implementação                                                                                                                                                                               |
| ----------------------------- | -------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| RESTful Design                | [Seção 2 — MELHORES-PRATICAS-API.md](MELHORES-PRATICAS-API.md) | [ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs)                                                                                                  |
| HTTP Verbs + Rate Limiting    | [Seções 3 e 8](MELHORES-PRATICAS-API.md)                       | [ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs) + [RateLimitingExtensions.cs](../src/Catalogo/Catalogo.API/Extensions/RateLimitingExtensions.cs) |
| Paginação                     | [Seção 2](MELHORES-PRATICAS-API.md)                            | [ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs)                                                                                                        |
| Versionamento                 | [Seção 6](MELHORES-PRATICAS-API.md)                            | `/api/v1/catalogo/` prefix em todos os endpoints                                                                                                                                            |
| Segurança JWT                 | [Seção 4](MELHORES-PRATICAS-API.md)                            | [AuthEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Auth/AuthEndpoints.cs)                                                                                                            |
| Validação FluentValidation    | [Seção 7](MELHORES-PRATICAS-API.md)                            | [ProdutoValidator.cs](../src/Catalogo/Catalogo.Application/Validators/ProdutoValidator.cs)                                                                                                  |
| Tratamento de Erros           | [Seção 5](MELHORES-PRATICAS-API.md)                            | [ExceptionHandlingMiddleware.cs](../src/Shared/Middleware/ExceptionHandlingMiddleware.cs)                                                                                                   |
| Idempotência                  | [Seção 3](MELHORES-PRATICAS-API.md)                            | [IdempotencyMiddleware.cs](../src/Shared/Middleware/IdempotencyMiddleware.cs)                                                                                                               |
| Logging                       | [Seção 9 — MELHORES-PRATICAS-API.md](MELHORES-PRATICAS-API.md) | [ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs)                                                                                                        |
| Rate Limiting                 | [Seção 8](MELHORES-PRATICAS-API.md)                            | [RateLimitingExtensions.cs](../src/Catalogo/Catalogo.API/Extensions/RateLimitingExtensions.cs)                                                                                              |
| Domínio Rico + Result Pattern | [docs/03-PEDIDOS.md](../docs/03-PEDIDOS.md)                    | [Pedidos/Domain/](../src/Pedidos/Domain/)                                                                                                                                                   |

---

## Checklist Final

Verificar se todas as práticas foram implementadas:

- ✅ Endpoints seguem convenção RESTful (`/api/v1/catalogo/produtos`, substantivos no plural)
- ✅ Versionamento em URL (`/api/v1/`)
- ✅ Autenticação JWT Bearer (escrita exige token, leitura é anônima)
- ✅ Validação com FluentValidation (CriarProdutoValidator, AtualizarProdutoValidator)
- ✅ Erros retornam status codes corretos (200, 201, 204, 400, 401, 404, 409, 422, 429, 500)
- ✅ Tratamento global de exceções (ExceptionHandlingMiddleware)
- ✅ Logging estruturado com Serilog
- ✅ Documentação com Swagger/OpenAPI
- ✅ Async/Await em todas as operações I/O
- ✅ Paginação implementada (page, pageSize, TotalPages no envelope)
- ✅ CORS configurado
- ✅ DTOs separados de entidades (CriarProdutoRequest ≠ Produto ≠ ProdutoResponse)
- ✅ Repositórios abstraídos por interfaces (Query/Command segregados)
- ✅ EF Core com LINQ parametrizado (proteção contra SQL Injection)
- ✅ **Rate limiting com 3 políticas** (leitura/escrita/criacao-produto)
- ✅ **Soft delete** (DELETE seta `Ativo = false`; produto inativo → 404 em todos os endpoints)
- ✅ **Idempotência** via `IdempotencyMiddleware` + header `Idempotency-Key`
- ✅ **Domínio rico no Catálogo** (Produto.Criar(), Categoria, Variante com value objects)
- ✅ **Result pattern** em Pedidos (sem exceptions para erros de negócio)

---

---

## 10.1 Rate Limiting — Implementação no Catálogo

**Referência**: [MELHORES-PRATICAS-API.md — Seção 8](MELHORES-PRATICAS-API.md)

O Catálogo usa três políticas com algoritmos distintos para diferentes perfis de operação:

### Registro das políticas

```csharp
// src/Catalogo/Catalogo.API/Extensions/RateLimitingExtensions.cs
public static IServiceCollection AddCatalogoRateLimiting(this IServiceCollection services)
{
    services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, ct) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString();
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(
                """{"erro":"Too Many Requests"}""", ct);
        };

        options.AddFixedWindowLimiter("leitura", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 60;
            opt.QueueLimit = 0;
        });

        options.AddSlidingWindowLimiter("escrita", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.SegmentsPerWindow = 6;    // suaviza rajadas em segmentos de 10s
            opt.PermitLimit = 20;
            opt.QueueLimit = 0;
        });

        options.AddTokenBucketLimiter("criacao-produto", opt =>
        {
            opt.TokenLimit = 5;
            opt.TokensPerPeriod = 5;
            opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 0;
        });
    });
    return services;
}
```

### Por que não registrar em `Testing`?

```csharp
// Program.cs
if (!app.Environment.IsEnvironment("Testing"))
    app.AddCatalogoRateLimiting();
```

O `WebApplicationFactory` chama `CreateHost()` depois que a aplicação já foi construída. Se `Program.cs` registrasse as políticas e depois a factory tentasse registrá-las novamente com outros limites, ocorreria `InvalidOperationException` por chave duplicada. A solução é não registrar em Testing e deixar cada factory definir suas próprias políticas.

### Factory de testes de rate limiting

```csharp
// tests/ProdutosAPI.Tests/Integration/RateLimitingApiFactory.cs
public class RateLimitingApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            // Substitui o registro padrão por limites baixos para testes
            services.AddCatalogoRateLimitingWithLimits(
                leituraLimit: 3,
                escritaLimit: 3,
                criacaoProdutoLimit: 2
            );
        });
    }
}
```

### Teste de rate limiting

```csharp
// tests/ProdutosAPI.Tests/Integration/RateLimitingTests.cs
public class RateLimitingTests : IClassFixture<RateLimitingApiFactory>
{
    [Fact]
    public async Task CriacaoProduto_ExcedeTokenBucket_Retorna429()
    {
        var client = await CriarClienteAutenticadoAsync();

        // Política criacao-produto tem limite 2 no RateLimitingApiFactory
        for (var i = 0; i < 2; i++)
            await client.PostAsJsonAsync("/api/v1/catalogo/produtos", ProdutoValido(i));

        // 3ª requisição deve ser rejeitada
        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", ProdutoValido(99));
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("Retry-After").Should().BeTrue();
    }
}
```

---

## Capítulo Extra: Vertical Slice + Domínio Rico (Pedidos)

O projeto utiliza **Vertical Slice Architecture** para o caso de uso de Pedidos, um padrão em que cada operação é auto-contida em sua própria pasta com
componente Command/Handler/Validator/Endpoint. O objetivo é reduzir acoplamento e melhorar a navegabilidade em APIs mais complexas.

### Anatomia de um Slice

Cada slice está localizado em `src/Pedidos/<Operação>/`:

```
src/Pedidos/CreatePedido/
├─ CreatePedidoCommand.cs      # DTO de entrada
├─ CreatePedidoValidator.cs    # FluentValidation do comando
├─ CreatePedidoHandler.cs      # Lógica de negócio (usa domínio rico)
└─ CreatePedidoEndpoint.cs     # Mapeia rota e resultados
```

Os endpoints são registrados automaticamente por scan de `IEndpoint` no startup:

```csharp
builder.Services.AddEndpointsFromAssembly(typeof(Program).Assembly);
```

### Domínio Rico

O agregado `Pedido` reside em `src/Pedidos/Domain/` e encapsula regras:

```csharp
public sealed class Pedido
{
    private readonly List<PedidoItem> _itens = new();
    public Result AddItem(PedidoItem novo)
    {
        if (novo.Preco <= 0) return Result.Fail("Preço inválido");
        if (_itens.Sum(i => i.Total) + novo.Total > Limite)
            return Result.Fail("Total excede limite");
        _itens.Add(novo);
        return Result.Ok();
    }
    // outras invariantes
}
```

Todos os métodos retornam `Result<T>` em vez de lançar exceções, seguindo a **Result Pattern**.

### Quando usar este padrão?

- Recursos com várias operações independentes
- Projetos que crescerão em escala
- Deseja-se manter cada caso de uso isolado e testável

Os slices coexistem pacificamente com os endpoints de Produtos baseados em camadas; ambos compartilham o mesmo contexto de dados e pipeline de middleware.
