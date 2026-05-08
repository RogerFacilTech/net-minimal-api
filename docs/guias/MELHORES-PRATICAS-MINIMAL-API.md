# Melhores PrÃ¡ticas de Minimal API em .NET - Guia de ImplementaÃ§Ã£o

## IntroduÃ§Ã£o

Este documento explica como as melhores prÃ¡ticas de API REST apresentadas em `MELHORES-PRATICAS-API.md` foram implementadas no projeto de exemplo usando .NET 10 e Minimal API.

## VersÃ£o do .NET

- **.NET 10.0** - VersÃ£o LTS mais moderna com suporte estendido
- **Minimal API** - Abordagem simplificada para criar APIs sem controllers

### Por que Minimal API?

A Minimal API Ã© ideal para:

- âœ… APIs simples e diretas
- âœ… MicroserviÃ§os
- âœ… APIs com poucos endpoints
- âœ… Prototipagem rÃ¡pida
- âœ… Menor overhead de framework

---

## Estrutura do Projeto

```
net-minimal-api/
â”œâ”€â”€ src/
â”‚   â”œâ”€â”€ Catalogo/                           # Bounded Context 1 â€” Clean Architecture hÃ­brida
â”‚   â”‚   â”œâ”€â”€ Catalogo.Domain/                # Entidades, value objects, interfaces de repositÃ³rio
â”‚   â”‚   â”‚   â”œâ”€â”€ Produto.cs                  # Aggregate com Produto.Criar() â†’ Result<Produto>
â”‚   â”‚   â”‚   â”œâ”€â”€ Categoria.cs               # Slug gerado, hierarquia pai/filho
â”‚   â”‚   â”‚   â”œâ”€â”€ Variante.cs                # SKU value object
â”‚   â”‚   â”‚   â”œâ”€â”€ Atributo.cs / Midia.cs     # CRUD simples (anÃªmico)
â”‚   â”‚   â”‚   â””â”€â”€ Common/                    # PrecoProduto, EstoqueProduto, DomainResult
â”‚   â”‚   â”œâ”€â”€ Catalogo.Application/
â”‚   â”‚   â”‚   â”œâ”€â”€ Services/                  # OrquestraÃ§Ã£o â€” ProdutoService, etc.
â”‚   â”‚   â”‚   â”œâ”€â”€ DTOs/                      # Produto/, Categoria/, Variante/, etc.
â”‚   â”‚   â”‚   â”œâ”€â”€ Validators/                # FluentValidation por recurso
â”‚   â”‚   â”‚   â”œâ”€â”€ Repositories/              # Interfaces Query/Command (CQRS leve)
â”‚   â”‚   â”‚   â””â”€â”€ Mappings/                  # AutoMapper profiles
â”‚   â”‚   â”œâ”€â”€ Catalogo.Infrastructure/       # RepositÃ³rios EF Core, DbSeeder
â”‚   â”‚   â”œâ”€â”€ Catalogo.API/
â”‚   â”‚   â”‚   â”œâ”€â”€ Endpoints/                 # Um arquivo por recurso
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ Produtos/ProdutoEndpoints.cs
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ Categorias/CategoriaEndpoints.cs
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ Variantes/VarianteEndpoints.cs
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ Atributos/AtributoEndpoints.cs
â”‚   â”‚   â”‚   â”‚   â”œâ”€â”€ Midias/MidiaEndpoints.cs
â”‚   â”‚   â”‚   â”‚   â””â”€â”€ Auth/AuthEndpoints.cs
â”‚   â”‚   â”‚   â””â”€â”€ Extensions/
â”‚   â”‚   â”‚       â””â”€â”€ RateLimitingExtensions.cs  # 3 polÃ­ticas
â”‚   â”‚   â””â”€â”€ Catalogo.ClientDemo/           # Console app â€” resiliÃªncia Polly v8
â”‚   â”‚
â”‚   â”œâ”€â”€ Pedidos/                           # Bounded Context 2 â€” Vertical Slice + DomÃ­nio Rico
â”‚   â”‚   â”œâ”€â”€ Domain/                        # Pedido aggregate, Result<T>
â”‚   â”‚   â””â”€â”€ Features/                      # CreatePedido/, GetPedido/, etc.
â”‚   â”‚
â”‚   â”œâ”€â”€ Pix/                               # Bounded Context 3 â€” Mock + Cliente HTTP
â”‚   â”‚   â”œâ”€â”€ Pix.MockServer/                # Simula BCB Pix (OAuth2 + mTLS)
â”‚   â”‚   â””â”€â”€ Pix.ClientDemo/                # HttpClient tipado com resiliÃªncia
â”‚   â”‚
â”‚   â””â”€â”€ Shared/
â”‚       â”œâ”€â”€ Common/                        # IEndpoint, Result<T>, EndpointExtensions
â”‚       â”œâ”€â”€ Data/                          # AppDbContext + Migrations + DbSeeder
â”‚       â””â”€â”€ Middleware/                    # ExceptionHandling, Idempotency
â”‚
â””â”€â”€ tests/
    â”œâ”€â”€ FacShopAPI.Tests/                 # 143 testes â€” CatÃ¡logo e Pedidos
    â””â”€â”€ Pix.MockServer.Tests/              # 7 testes â€” integraÃ§Ã£o PIX
```

---

## ImplementaÃ§Ã£o das Melhores PrÃ¡ticas

### Boas prÃ¡ticas de cliente HTTP (integraÃ§Ãµes externas)

AlÃ©m dos endpoints internos, o projeto tambÃ©m demonstra consumo de API externa simulada:

- `HttpClientFactory` com cliente tipado (`PixProcessingClient`);
- `AddStandardResilienceHandler` para retry e timeout;
- `DelegatingHandler` para `X-Correlation-Id`, `Idempotency-Key` e logging;
- `AuthTokenProvider` com cache de token OAuth2 mock.

ReferÃªncias:

- [samples/Pix/Pix.ClientDemo/Program.cs](../samples/Pix/Pix.ClientDemo/Program.cs)
- [samples/Pix/Pix.ClientDemo/Client/PixProcessingClient.cs](../samples/Pix/Pix.ClientDemo/Client/PixProcessingClient.cs)
- [samples/Pix/Pix.ClientDemo/Client/AuthTokenProvider.cs](../samples/Pix/Pix.ClientDemo/Client/AuthTokenProvider.cs)

### 1. RESTful Design

#### âœ… IdentificaÃ§Ã£o de Recursos

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "RESTful Design"

Os endpoints seguem a convenÃ§Ã£o REST com recursos bem definidos:

```csharp
// src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs
// prefixo definido no MapGroup do CatÃ¡logo: /api/v1/catalogo/produtos

// Recursos identificados por URI
GET    /api/v1/catalogo/produtos              â†’ Listar produtos
GET    /api/v1/catalogo/produtos/{id}         â†’ Obter especÃ­fico
POST   /api/v1/catalogo/produtos              â†’ Criar novo
PUT    /api/v1/catalogo/produtos/{id}         â†’ Atualizar completo
PATCH  /api/v1/catalogo/produtos/{id}         â†’ Atualizar parcial
DELETE /api/v1/catalogo/produtos/{id}         â†’ Deletar
```

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L15-L20)

#### âœ… OperaÃ§Ãµes PadrÃ£o HTTP

Cada endpoint usa o verbo HTTP correto:

```csharp
// POST - Criar (custo alto â†’ polÃ­tica mais restritiva)
group.MapPost("/", CriarProduto)
    .WithName("CriarProduto")
    .Produces<ProdutoResponse>(StatusCodes.Status201Created)
    .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
    .RequireAuthorization()
    .RequireRateLimiting("criacao-produto");    // TokenBucket, 5 req/min

// GET - Recuperar (idempotente, anÃ´nimo, rate limit suave)
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

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L29-L60)

#### âœ… RepresentaÃ§Ã£o Padronizada

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "RepresentaÃ§Ã£o de Recursos"

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

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.Application/DTOs/ProdutoDTO.cs](../src/Catalogo/Catalogo.Application/DTOs/ProdutoDTO.cs#L25)

#### âœ… Statelessness

Cada requisiÃ§Ã£o deve conter todas as informaÃ§Ãµes necessÃ¡rias:

```csharp
// NÃ£o mantÃ©m estado de sessÃ£o
// AutenticaÃ§Ã£o futura: JWT token em header Authorization

private static async Task<IResult> ListarProdutos(
    IProdutoService produtoService,
    int page = 1,
    int pageSize = 20,
    string? categoria = null,
    string? search = null)
{
    // Toda informaÃ§Ã£o estÃ¡ na requisiÃ§Ã£o
    var resultado = await produtoService.ListarProdutosAsync(
        page, pageSize, categoria, search);

    return Results.Ok(resultado);
}
```

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L70-L80)

---

### 2. Design de Endpoints

#### âœ… Nomenclatura de URLs

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "Nomenclatura de URLs"

```csharp
// âœ… CORRETO: Nomes em plural
GET /api/v1/catalogo/produtos

// âœ… CORRETO: MinÃºsculas
GET /api/v1/catalogo/produtos/123

// âœ… CORRETO: HÃ­fens para separar palavras
GET /api/v1/catalogo/produtos?status=produto-ativo

// âŒ EVITAR: Verbos nas URLs
// GET /api/v1/obter-produtos
// GET /api/v1/deletar-produto/123
```

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L14-L15)

#### âœ… PaginaÃ§Ã£o

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "PaginaÃ§Ã£o"

```csharp
// RequisiÃ§Ã£o com paginaÃ§Ã£o
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

**ImplementaÃ§Ã£o da resposta**: [src/Catalogo/Catalogo.Application/DTOs/ProdutoDTO.cs](../src/Catalogo/Catalogo.Application/DTOs/ProdutoDTO.cs#L46-L57)

**ImplementaÃ§Ã£o do endpoint**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L70-L86)

**ImplementaÃ§Ã£o do serviÃ§o**:

```csharp
// src/Catalogo/Catalogo.Application/Services/ProdutoService.cs
public async Task<PaginatedResponse<ProdutoResponse>> ListarProdutosAsync(
    int page, int pageSize, string? categoria = null, string? search = null)
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20; // MÃ¡ximo 100

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

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L32-L75)

#### âœ… Filtros e Busca

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "Filtros e Busca"

```csharp
// Suporte a filtros e busca na mesma requisiÃ§Ã£o
GET /api/v1/catalogo/produtos?categoria=eletrÃ´nicos&search=notebook

// No serviÃ§o:
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

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L48-L55)

---

### 3. Versionamento

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "Versionamento"

#### âœ… URL Path Versionamento (Recomendado)

```csharp
// Program.cs
// prefixo definido no MapGroup do CatÃ¡logo: /api/v1/catalogo/produtos

// Endpoints comeÃ§am com /api/v1/
// FÃ¡cil evoluir para /api/v2/ no futuro
```

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L14)

#### âœ… Versionamento SemÃ¢ntico do Projeto

```xml
<!-- FacShopAPI.csproj -->
<Version>1.0.0</Version>
<Description>API REST de Produtos com Minimal API e .NET 10</Description>
```

**ImplementaÃ§Ã£o**: [FacShopAPI.csproj](FacShopAPI.csproj#L8-L9)

---

### 4. SeguranÃ§a

#### âœ… ValidaÃ§Ã£o de Inputs

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "SeguranÃ§a - ValidaÃ§Ã£o"

Usando **FluentValidation** para validaÃ§Ãµes robustas:

```csharp
// src/Catalogo/Catalogo.Application/Validators/ProdutoValidator.cs
public class CriarProdutoValidator : AbstractValidator<CriarProdutoRequest>
{
    public CriarProdutoValidator()
    {
        RuleFor(p => p.Nome)
            .NotEmpty()
            .WithMessage("Nome Ã© obrigatÃ³rio")
            .MinimumLength(3)
            .WithMessage("Nome deve ter no mÃ­nimo 3 caracteres")
            .MaximumLength(100)
            .WithMessage("Nome nÃ£o pode exceder 100 caracteres");

        RuleFor(p => p.Preco)
            .GreaterThan(0)
            .WithMessage("PreÃ§o deve ser maior que zero");

        RuleFor(p => p.ContatoEmail)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Email de contato invÃ¡lido");
    }
}
```

**ImplementaÃ§Ã£o do validador**: [src/Catalogo/Catalogo.Application/Validators/ProdutoValidator.cs](../src/Catalogo/Catalogo.Application/Validators/ProdutoValidator.cs)

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

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L125-L145)

#### âœ… ProteÃ§Ã£o contra SQL Injection

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "SeguranÃ§a - SQL Injection"

Usando **Entity Framework Core** (ORM) ao invÃ©s de SQL raw:

```csharp
// âœ… SEGURO: Usando EF Core com queries LINQ
var produtos = await _context.Produtos
    .Where(p => p.Nome.Contains(search)) // Parametrizado automaticamente
    .ToListAsync();

// âŒ NÃƒO FAZER: Raw SQL sem parametrizaÃ§Ã£o
// var produtos = _context.Produtos.FromSqlRaw(
//     $"SELECT * FROM Produtos WHERE Nome LIKE '%{search}%'");
```

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L48)

#### âœ… CORS Configurado

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "SeguranÃ§a - CORS"

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

**ImplementaÃ§Ã£o**: [Program.cs](Program.cs#L42-L51)

---

### 5. ValidaÃ§Ã£o de Dados

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "ValidaÃ§Ã£o de Dados"

#### âœ… Input Validation

Campos validados conforme business rules:

```csharp
// src/Catalogo/Catalogo.Domain/Produto.cs
public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;     // Min 3 caracteres
    public decimal Preco { get; set; }                   // Deve ser > 0
    public int Estoque { get; set; }                     // NÃ£o pode ser negativo
    public string ContatoEmail { get; set; } = string.Empty; // Email vÃ¡lido
    public bool Ativo { get; set; } = true;              // Status soft delete
}
```

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.Domain/Produto.cs](../src/Catalogo/Catalogo.Domain/Produto.cs)

#### âœ… Mensagens de Erro de ValidaÃ§Ã£o

```csharp
// Resposta de validaÃ§Ã£o
{
  "errors": {
    "nome": ["Campo obrigatÃ³rio", "MÃ­nimo 3 caracteres"],
    "preco": ["Deve ser maior que 0"],
    "email": ["Email invÃ¡lido"]
  }
}
```

**ImplementaÃ§Ã£o**: [src/Shared/Middleware/ExceptionHandlingMiddleware.cs](../src/Shared/Middleware/ExceptionHandlingMiddleware.cs#L47-L60)

#### âœ… SanitizaÃ§Ã£o

AutoMapper e FluentValidation garantem sanitizaÃ§Ã£o:

```csharp
// AutoMapper mapeia e converte tipos
CreateMap<CriarProdutoRequest, Produto>();

// FluentValidation valida formato
RuleFor(p => p.ContatoEmail)
    .EmailAddress()
    .WithMessage("Email de contato invÃ¡lido");
```

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.Application/Mappings/ProdutoMappingProfile.cs](../src/Catalogo/Catalogo.Application/Mappings/ProdutoMappingProfile.cs)

---

### 6. Tratamento de Erros

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "Tratamento de Erros"

#### âœ… HTTP Status Codes Corretos

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

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L88-L180)

#### âœ… Respostas de Erro Padronizadas

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "Resposta de Erro Padronizada"

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
        "nome": ["Campo obrigatÃ³rio"],
        "preco": ["Deve ser maior que 0"]
    }
}
```

**ImplementaÃ§Ã£o**: [src/Shared/Middleware/ExceptionHandlingMiddleware.cs](../src/Shared/Middleware/ExceptionHandlingMiddleware.cs#L35-L75)

#### âœ… Middleware Global de Tratamento de Erros

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
            _logger.LogError(ex, "ExceÃ§Ã£o nÃ£o tratada");
            await HandleExceptionAsync(context, ex);
        }
    }
}

// Program.cs
app.UseExceptionHandling();
```

**ImplementaÃ§Ã£o**: [src/Shared/Middleware/ExceptionHandlingMiddleware.cs](../src/Shared/Middleware/ExceptionHandlingMiddleware.cs)

---

### 7. DocumentaÃ§Ã£o

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "DocumentaÃ§Ã£o"

#### âœ… OpenAPI/Swagger

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

**ImplementaÃ§Ã£o**: [Program.cs](Program.cs#L80-L100)

#### âœ… Endpoints com DescriÃ§Ã£o

```csharp
// src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs
group.MapGet("/", ListarProdutos)
    .WithName("ListarProdutos")
    .WithDescription("Lista todos os produtos com paginaÃ§Ã£o")
    .WithSummary("Listar produtos")
    .Produces<PaginatedResponse<ProdutoResponse>>(StatusCodes.Status200OK)
    .AllowAnonymous();
```

**Acesso**: http://localhost:5001 (Swagger UI)

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs#L22-L27)

#### âœ… XML Comments

```csharp
// src/Catalogo/Catalogo.Domain/Produto.cs
/// <summary>
/// Entidade Produto
/// ReferÃªncia: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "Design de Endpoints"
/// Representa um produto no sistema
/// </summary>
public class Produto
{
    /// <summary>
    /// Identificador Ãºnico do produto (PK)
    /// </summary>
    public int Id { get; set; }
}
```

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.Domain/Produto.cs](../src/Catalogo/Catalogo.Domain/Produto.cs#L1)

---

### 8. Performance

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "Performance"

#### âœ… PaginaÃ§Ã£o ObrigatÃ³ria

```csharp
// MÃ¡ximo 100 itens por pÃ¡gina
if (pageSize < 1 || pageSize > 100) pageSize = 20;

// PadrÃ£o: 20 itens
int pageSize = 20
```

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L41-L42)

#### âœ… Async/Await

```csharp
// Todas as operaÃ§Ãµes I/O sÃ£o assÃ­ncronas
public async Task<PaginatedResponse<ProdutoResponse>> ListarProdutosAsync(...)
{
    var totalItems = await query.CountAsync();
    var produtos = await query.ToListAsync();
    // ...
}
```

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L32)

#### âœ… Ãndices de Banco de Dados

```csharp
// src/Shared/Data/AppDbContext.cs
entity.HasIndex(p => p.Ativo)
    .HasName("idx_produto_ativo");

entity.HasIndex(p => p.Categoria)
    .HasName("idx_produto_categoria");
```

**ImplementaÃ§Ã£o**: [src/Shared/Data/AppDbContext.cs](../src/Shared/Data/AppDbContext.cs#L26-L30)

---

### 9. Logging e Monitoramento

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "Logging e Monitoramento"

#### âœ… Structured Logging com Serilog

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

**ImplementaÃ§Ã£o**: [Program.cs](Program.cs#L17-L33)

#### âœ… Logging em ServiÃ§os

```csharp
// src/Catalogo/Catalogo.Application/Services/ProdutoService.cs
_logger.LogInformation("Listando produtos - Page: {Page}, PageSize: {PageSize}",
    page, pageSize);

_logger.LogWarning("Produto com ID {ProductId} nÃ£o encontrado", id);

_logger.LogError(ex, "Erro ao listar produtos");
```

**ImplementaÃ§Ã£o**: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs#L34)

#### âœ… CorrelaÃ§Ã£o de RequisiÃ§Ãµes

```csharp
// Middleware captura request ID
var requestId = context.TraceIdentifier;

// DisponÃ­vel em logs
_logger.LogInformation("RequisiÃ§Ã£o: {RequestId}", requestId);
```

---

### 10. Testes

**ReferÃªncia**: MELHORES-PRATICAS-API.md - SeÃ§Ã£o "Testes"

Para implementar testes, crie um projeto de teste:

```bash
# Criar projeto de teste
dotnet new xunit --name FacShopAPI.Tests

# Adicionar referÃªncias
dotnet add package Moq
dotnet add package FluentAssertions
```

#### âœ… Exemplo de Teste UnitÃ¡rio

```csharp
// tests/FacShopAPI.Tests/Services/ProdutoServiceTests.cs
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

### PrÃ©-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQLite Ã© gerenciado automaticamente pelo EF Core

### Trilha CatÃ¡logo (API principal)

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

# 4. Listar produtos (anÃ´nimo)
curl "http://localhost:5001/api/v1/catalogo/produtos?page=1&pageSize=10"

# 5. Criar produto (requer JWT)
curl -X POST "http://localhost:5001/api/v1/catalogo/produtos" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Notebook Dell",
    "descricao": "Notebook de alto desempenho, 16GB RAM",
    "preco": 3500.00,
    "categoria": "EletrÃ´nicos",
    "estoque": 5,
    "contatoEmail": "vendas@dell.com"
  }'

# 6. Soft delete (produto vira 404 apÃ³s deleÃ§Ã£o)
curl -X DELETE "http://localhost:5001/api/v1/catalogo/produtos/1" \
  -H "Authorization: Bearer $TOKEN"

# 7. GET apÃ³s soft delete â†’ 404
curl "http://localhost:5001/api/v1/catalogo/produtos/1"

# 8. Observar rate limiting (apÃ³s 5 POSTs rÃ¡pidos na polÃ­tica criacao-produto â†’ 429)
for i in {1..6}; do
  curl -s -o /dev/null -w "Request $i: %{http_code}\n" \
    -X POST "http://localhost:5001/api/v1/catalogo/produtos" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"nome":"Produto '$i'","descricao":"Desc produto '$i'","preco":10,"categoria":"Outros","estoque":1,"contatoEmail":"t@t.com"}'
done
# Primeiras 5: 201 Created; 6Âª: 429 Too Many Requests + header Retry-After
```

### Trilha PIX (integraÃ§Ã£o externa)

```bash
# Terminal 1 â€” servidor mock
dotnet run --project samples/Pix/Pix.MockServer/Pix.MockServer.csproj

# Terminal 2 â€” cliente didÃ¡tico
dotnet run --project samples/Pix/Pix.ClientDemo/Pix.ClientDemo.csproj
```

### Rodar testes

```bash
dotnet test FacShopAPI.slnx -v minimal            # todos os 150 testes
dotnet test tests/FacShopAPI.Tests/ \
  --filter "FullyQualifiedName~RateLimitingTests"  # sÃ³ rate limiting
```

---

## Estrutura de Pastas (CatÃ¡logo)

```
src/Catalogo/
â”œâ”€â”€ Catalogo.Domain/
â”‚   â”œâ”€â”€ Produto.cs                  â† Aggregate rico: Produto.Criar() â†’ Result<Produto>
â”‚   â”œâ”€â”€ Categoria.cs               â† Hierarquia + slug auto-gerado
â”‚   â”œâ”€â”€ Variante.cs                â† SKU value object (sealed record)
â”‚   â”œâ”€â”€ Atributo.cs / Midia.cs     â† CRUD simples (anÃªmico, sem invariantes)
â”‚   â””â”€â”€ Common/
â”‚       â”œâ”€â”€ PrecoProduto.cs        â† Value object: preÃ§o nÃ£o negativo
â”‚       â”œâ”€â”€ EstoqueProduto.cs      â† Value object: estoque nÃ£o negativo
â”‚       â””â”€â”€ DomainResult.cs        â† Result<T> do domÃ­nio
â”‚
â”œâ”€â”€ Catalogo.Application/
â”‚   â”œâ”€â”€ Services/
â”‚   â”‚   â”œâ”€â”€ IProdutoService.cs     â† Interface do serviÃ§o
â”‚   â”‚   â””â”€â”€ ProdutoService.cs      â† OrquestraÃ§Ã£o + logging + paginaÃ§Ã£o
â”‚   â”œâ”€â”€ DTOs/
â”‚   â”‚   â””â”€â”€ Produto/
â”‚   â”‚       â””â”€â”€ ProdutoDTO.cs      â† CriarProdutoRequest, AtualizarProdutoRequest, ProdutoResponse
â”‚   â”œâ”€â”€ Validators/
â”‚   â”‚   â””â”€â”€ ProdutoValidator.cs    â† CriarProdutoValidator, AtualizarProdutoValidator
â”‚   â”œâ”€â”€ Repositories/
â”‚   â”‚   â”œâ”€â”€ IProdutoQueryRepository.cs   â† Retorna DTOs diretamente (leitura)
â”‚   â”‚   â””â”€â”€ IProdutoCommandRepository.cs â† Opera sobre entidades (escrita)
â”‚   â””â”€â”€ Mappings/
â”‚       â””â”€â”€ ProdutoMappingProfile.cs
â”‚
â”œâ”€â”€ Catalogo.Infrastructure/
â”‚   â”œâ”€â”€ Repositories/              â† ImplementaÃ§Ãµes EF Core das interfaces
â”‚   â””â”€â”€ Data/DbSeeder.cs           â† 8 produtos e 5 categorias (IDs 1-8 / 1-5 reservados)
â”‚
â””â”€â”€ Catalogo.API/
    â”œâ”€â”€ Endpoints/
    â”‚   â””â”€â”€ Produtos/ProdutoEndpoints.cs   â† 6 rotas com RequireRateLimiting
    â””â”€â”€ Extensions/
        â””â”€â”€ RateLimitingExtensions.cs      â† leitura / escrita / criacao-produto
```

---

## ReferÃªncias Cruzadas

| Aspecto                       | Guia teÃ³rico                                                   | ImplementaÃ§Ã£o                                                                                                                                                                               |
| ----------------------------- | -------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| RESTful Design                | [SeÃ§Ã£o 2 â€” MELHORES-PRATICAS-API.md](MELHORES-PRATICAS-API.md) | [ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs)                                                                                                  |
| HTTP Verbs + Rate Limiting    | [SeÃ§Ãµes 3 e 8](MELHORES-PRATICAS-API.md)                       | [ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs) + [RateLimitingExtensions.cs](../src/Catalogo/Catalogo.API/Extensions/RateLimitingExtensions.cs) |
| PaginaÃ§Ã£o                     | [SeÃ§Ã£o 2](MELHORES-PRATICAS-API.md)                            | [ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs)                                                                                                        |
| Versionamento                 | [SeÃ§Ã£o 6](MELHORES-PRATICAS-API.md)                            | `/api/v1/catalogo/` prefix em todos os endpoints                                                                                                                                            |
| SeguranÃ§a JWT                 | [SeÃ§Ã£o 4](MELHORES-PRATICAS-API.md)                            | [AuthEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Auth/AuthEndpoints.cs)                                                                                                            |
| ValidaÃ§Ã£o FluentValidation    | [SeÃ§Ã£o 7](MELHORES-PRATICAS-API.md)                            | [ProdutoValidator.cs](../src/Catalogo/Catalogo.Application/Validators/ProdutoValidator.cs)                                                                                                  |
| Tratamento de Erros           | [SeÃ§Ã£o 5](MELHORES-PRATICAS-API.md)                            | [ExceptionHandlingMiddleware.cs](../src/Shared/Middleware/ExceptionHandlingMiddleware.cs)                                                                                                   |
| IdempotÃªncia                  | [SeÃ§Ã£o 3](MELHORES-PRATICAS-API.md)                            | [IdempotencyMiddleware.cs](../src/Shared/Middleware/IdempotencyMiddleware.cs)                                                                                                               |
| Logging                       | [SeÃ§Ã£o 9 â€” MELHORES-PRATICAS-API.md](MELHORES-PRATICAS-API.md) | [ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs)                                                                                                        |
| Rate Limiting                 | [SeÃ§Ã£o 8](MELHORES-PRATICAS-API.md)                            | [RateLimitingExtensions.cs](../src/Catalogo/Catalogo.API/Extensions/RateLimitingExtensions.cs)                                                                                              |
| DomÃ­nio Rico + Result Pattern | [docs/03-PEDIDOS.md](../docs/03-PEDIDOS.md)                    | [Pedidos/Domain/](../src/Pedidos/Domain/)                                                                                                                                                   |

---

## Checklist Final

Verificar se todas as prÃ¡ticas foram implementadas:

- âœ… Endpoints seguem convenÃ§Ã£o RESTful (`/api/v1/catalogo/produtos`, substantivos no plural)
- âœ… Versionamento em URL (`/api/v1/`)
- âœ… AutenticaÃ§Ã£o JWT Bearer (escrita exige token, leitura Ã© anÃ´nima)
- âœ… ValidaÃ§Ã£o com FluentValidation (CriarProdutoValidator, AtualizarProdutoValidator)
- âœ… Erros retornam status codes corretos (200, 201, 204, 400, 401, 404, 409, 422, 429, 500)
- âœ… Tratamento global de exceÃ§Ãµes (ExceptionHandlingMiddleware)
- âœ… Logging estruturado com Serilog
- âœ… DocumentaÃ§Ã£o com Swagger/OpenAPI
- âœ… Async/Await em todas as operaÃ§Ãµes I/O
- âœ… PaginaÃ§Ã£o implementada (page, pageSize, TotalPages no envelope)
- âœ… CORS configurado
- âœ… DTOs separados de entidades (CriarProdutoRequest â‰  Produto â‰  ProdutoResponse)
- âœ… RepositÃ³rios abstraÃ­dos por interfaces (Query/Command segregados)
- âœ… EF Core com LINQ parametrizado (proteÃ§Ã£o contra SQL Injection)
- âœ… **Rate limiting com 3 polÃ­ticas** (leitura/escrita/criacao-produto)
- âœ… **Soft delete** (DELETE seta `Ativo = false`; produto inativo â†’ 404 em todos os endpoints)
- âœ… **IdempotÃªncia** via `IdempotencyMiddleware` + header `Idempotency-Key`
- âœ… **DomÃ­nio rico no CatÃ¡logo** (Produto.Criar(), Categoria, Variante com value objects)
- âœ… **Result pattern** em Pedidos (sem exceptions para erros de negÃ³cio)

---

---

## 10.1 Rate Limiting â€” ImplementaÃ§Ã£o no CatÃ¡logo

**ReferÃªncia**: [MELHORES-PRATICAS-API.md â€” SeÃ§Ã£o 8](MELHORES-PRATICAS-API.md)

O CatÃ¡logo usa trÃªs polÃ­ticas com algoritmos distintos para diferentes perfis de operaÃ§Ã£o:

### Registro das polÃ­ticas

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

### Por que nÃ£o registrar em `Testing`?

```csharp
// Program.cs
if (!app.Environment.IsEnvironment("Testing"))
    app.AddCatalogoRateLimiting();
```

O `WebApplicationFactory` chama `CreateHost()` depois que a aplicaÃ§Ã£o jÃ¡ foi construÃ­da. Se `Program.cs` registrasse as polÃ­ticas e depois a factory tentasse registrÃ¡-las novamente com outros limites, ocorreria `InvalidOperationException` por chave duplicada. A soluÃ§Ã£o Ã© nÃ£o registrar em Testing e deixar cada factory definir suas prÃ³prias polÃ­ticas.

### Factory de testes de rate limiting

```csharp
// tests/FacShopAPI.Tests/Integration/RateLimitingApiFactory.cs
public class RateLimitingApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            // Substitui o registro padrÃ£o por limites baixos para testes
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
// tests/FacShopAPI.Tests/Integration/RateLimitingTests.cs
public class RateLimitingTests : IClassFixture<RateLimitingApiFactory>
{
    [Fact]
    public async Task CriacaoProduto_ExcedeTokenBucket_Retorna429()
    {
        var client = await CriarClienteAutenticadoAsync();

        // PolÃ­tica criacao-produto tem limite 2 no RateLimitingApiFactory
        for (var i = 0; i < 2; i++)
            await client.PostAsJsonAsync("/api/v1/catalogo/produtos", ProdutoValido(i));

        // 3Âª requisiÃ§Ã£o deve ser rejeitada
        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", ProdutoValido(99));
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("Retry-After").Should().BeTrue();
    }
}
```

---

## CapÃ­tulo Extra: Vertical Slice + DomÃ­nio Rico (Pedidos)

O projeto utiliza **Vertical Slice Architecture** para o caso de uso de Pedidos, um padrÃ£o em que cada operaÃ§Ã£o Ã© auto-contida em sua prÃ³pria pasta com
componente Command/Handler/Validator/Endpoint. O objetivo Ã© reduzir acoplamento e melhorar a navegabilidade em APIs mais complexas.

### Anatomia de um Slice

Cada slice estÃ¡ localizado em `src/Pedidos/<OperaÃ§Ã£o>/`:

```
src/Pedidos/CreatePedido/
â”œâ”€ CreatePedidoCommand.cs      # DTO de entrada
â”œâ”€ CreatePedidoValidator.cs    # FluentValidation do comando
â”œâ”€ CreatePedidoHandler.cs      # LÃ³gica de negÃ³cio (usa domÃ­nio rico)
â””â”€ CreatePedidoEndpoint.cs     # Mapeia rota e resultados
```

Os endpoints sÃ£o registrados automaticamente por scan de `IEndpoint` no startup:

```csharp
builder.Services.AddEndpointsFromAssembly(typeof(Program).Assembly);
```

### DomÃ­nio Rico

O agregado `Pedido` reside em `src/Pedidos/Domain/` e encapsula regras:

```csharp
public sealed class Pedido
{
    private readonly List<PedidoItem> _itens = new();
    public Result AddItem(PedidoItem novo)
    {
        if (novo.Preco <= 0) return Result.Fail("PreÃ§o invÃ¡lido");
        if (_itens.Sum(i => i.Total) + novo.Total > Limite)
            return Result.Fail("Total excede limite");
        _itens.Add(novo);
        return Result.Ok();
    }
    // outras invariantes
}
```

Todos os mÃ©todos retornam `Result<T>` em vez de lanÃ§ar exceÃ§Ãµes, seguindo a **Result Pattern**.

### Quando usar este padrÃ£o?

- Recursos com vÃ¡rias operaÃ§Ãµes independentes
- Projetos que crescerÃ£o em escala
- Deseja-se manter cada caso de uso isolado e testÃ¡vel

Os slices coexistem pacificamente com os endpoints de Produtos baseados em camadas; ambos compartilham o mesmo contexto de dados e pipeline de middleware.
