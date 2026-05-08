# Guia: Melhores Práticas para Criação de APIs REST

Este é um guia conceitual desenvolvido **para todos os níveis**. Não vamos assumir que você já saiba arquitetura de software complexa. Aqui, explicaremos o _porquê_ e o _como_ das principais regras do desenvolvimento moderno de APIs.

Os exemplos de código abaixo são extraídos diretamente desta implementação.

---

## Índice

1. [O que é uma API REST?](#1-o-que-é-uma-api-rest)
2. [A Anatomia de uma URL e Design de Endpoints](#2-a-anatomia-de-uma-url)
3. [Os Verbos HTTP e a Idempotência (MUITO IMPORTANTE)](#3-os-verbos-http-e-a-idempotência)
4. [Segurança e JWT (JSON Web Tokens)](#4-segurança-e-jwt)
5. [Tratamento de Erros e Códigos HTTP](#5-tratamento-de-erros)
6. [Evolução e Versionamento](#6-evolução-e-versionamento)
7. [Performance e Resiliência](#7-performance-e-resiliência)

---

## 1. O que é uma API REST?

API significa _Application Programming Interface_ (Interface de Programação de Aplicação). Resumindo de forma bem prática, é como se fosse um **"garçom"** em um restaurante.

- **O Cliente:** É você (o aplicativo do celular, ou um site).
- **O Garçom (API):** É quem recebe o seu pedido.
- **A Cozinha:** É o Servidor / Banco de Dados onde a mágica acontece.

O garçom anota seu pedido e leva pra cozinha. A cozinha prepara, devolve para o garçom e ele entrega a você (o _Response_).

**Mas o que é REST?**
REST é um "estilo" ou conjunto de regras e acordos sobre como esse garçom deve se comportar. Um garçom que segue o padrão REST tenta atender a premissas básicas, sendo a principal delas o modelo _Stateless_ (O servidor não memoriza transações anteriores; se você pedir "mais um copo", ele precisa saber "um copo de que?").

---

## 2. A Anatomia de uma URL e Design de Endpoints

Endpoints devem representar recursos, como pastas no seu computador.
A grande regra do REST é: **Use Substantivos no plural e evite verbos na URL**.

- ❌ ERRADO: `/pegar-produtos-ativos`
- ❌ ERRADO: `/criarProduto`
- ✅ CORRETO: `/api/v1/produtos`

**Como fazemos ações se não usamos verbos na URL?**
Nós delegamos o "Verbo" para o próprio protocolo HTTP. É o Verbo da requisição que dirá o que queremos fazer naquela pasta `/produtos`.

Veja como este projeto define todos os endpoints de produtos em `src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs`:

```csharp
var group = catalogoGroup.MapGroup("/produtos")   // prefixo do grupo: /api/v1/catalogo
    .WithTags("Catálogo - Produtos");

group.MapGet("/",      ListarProdutos);           // GET    /api/v1/catalogo/produtos
group.MapGet("/{id}", ObterProduto);              // GET    /api/v1/catalogo/produtos/42
group.MapPost("/",    CriarProduto);              // POST   /api/v1/catalogo/produtos
group.MapPut("/{id}", AtualizarCompletoProduto);  // PUT    /api/v1/catalogo/produtos/42
group.MapPatch("/{id}", AtualizarParcialProduto); // PATCH  /api/v1/catalogo/produtos/42
group.MapDelete("/{id}", DeletarProduto);         // DELETE /api/v1/catalogo/produtos/42
```

Um único substantivo no plural (`/produtos`) + o verbo HTTP = operação completa e semântica.

### Paginação é inegociável

Nunca devolva o banco de dados inteiro. Sempre exija `page` e `pageSize`. Neste projeto, o handler de listagem recebe esses parâmetros como query string:

```csharp
// src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs
private static async Task<IResult> ListarProdutos(
    IProdutoService produtoService,
    int page = 1,
    int pageSize = 20,
    string? categoria = null,
    string? search = null)
{
    var resultado = await produtoService.ListarProdutosAsync(page, pageSize, categoria, search);
    return Results.Ok(resultado);
}
```

A resposta segue um envelope padronizado com metadados de navegação:

```csharp
// src/Catalogo/Catalogo.Application/DTOs/Produto/ProdutoDTO.cs
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
```

Exemplo de chamada: `GET /api/v1/catalogo/produtos?page=2&pageSize=20&categoria=Eletrônicos`

---

## 3. Os Verbos HTTP e a Idempotência

O método como o Cliente (aplicativo) chama o Servidor (API) muda de acordo com o Verbo da operação:

- **GET**: "Quero **ler** informações de `/produtos`."
- **POST**: "Quero **criar** uma informação nova em `/produtos`."
- **PUT**: "Quero **substituir/atualizar TUDO** no item `/produtos/123`."
- **PATCH**: "Quero **modificar uma coisinha pequena** no item `/produtos/123`."
- **DELETE**: "Quero **limpar/apagar** o item `/produtos/123`."

Veja como cada verbo é declarado com seus status codes esperados no projeto:

```csharp
// src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs
group.MapGet("/", ListarProdutos)
    .Produces<PaginatedResponse<ProdutoResponse>>(200)
    .AllowAnonymous()
    .RequireRateLimiting("leitura");           // FixedWindow, 60 req/min

group.MapGet("/{id}", ObterProduto)
    .Produces<ProdutoResponse>(200)
    .Produces<ErrorResponse>(404)
    .AllowAnonymous()
    .RequireRateLimiting("leitura");

group.MapPost("/", CriarProduto)
    .Produces<ProdutoResponse>(201)
    .Produces<ErrorResponse>(422)
    .RequireAuthorization()
    .RequireRateLimiting("criacao-produto");   // TokenBucket, 5 req/min

group.MapPut("/{id}", AtualizarCompletoProduto)
    .Produces<ProdutoResponse>(200)
    .Produces<ErrorResponse>(404)
    .Produces<ErrorResponse>(422)
    .RequireAuthorization()
    .RequireRateLimiting("escrita");           // SlidingWindow, 20 req/min

group.MapPatch("/{id}", AtualizarParcialProduto)
    .Produces<ProdutoResponse>(200)
    .Produces<ErrorResponse>(404)
    .Produces<ErrorResponse>(422)
    .RequireAuthorization()
    .RequireRateLimiting("escrita");

group.MapDelete("/{id}", DeletarProduto)
    .Produces(204)
    .Produces<ErrorResponse>(404)
    .RequireAuthorization()
    .RequireRateLimiting("escrita");
```

### O que raios é Idempotência?

Na matemática e na programação, **Idempotência é a propriedade de uma operação poder ser repetida várias vezes de forma idêntica e o resultado final no servidor não se alterar a partir da 2ª vez**.

**Exemplo da vida real (Controle Remoto da TV):**

- O botão "Aumentar Volume (+)" **NÃO É** idempotente. Se você apertar ele 5 vezes, o volume vai aumentar 5 vezes (efeitos colaterais continuam acumulando).
- O botão "Colocar no Canal 4" **É** idempotente. Você pode apertá-lo uma vez, ou apertá-lo 500 vezes sem parar... o resultado final será exatamente o mesmo: a TV estará no Canal 4.

**Na API:**

- **GET**: É Idempotente. Ler algo 100 vezes não altera o banco.
- **PUT** e **DELETE**: São Idempotentes. Deletar ou substituir um item dez vezes resulta no mesmo estado final.
- **POST (Criação): NÃO É IDEMPOTENTE!**
  Se você apertar "Comprar" num site e a sua internet piscar, o celular tentará reenviar a requisição. Sem defesas, **o sistema gerará duas compras e você será cobrado duas vezes**!

### Solução: IdempotencyMiddleware

Este projeto implementa um `IdempotencyMiddleware` em `src/Shared/Middleware/IdempotencyMiddleware.cs`. Quando o cliente envia o header `Idempotency-Key`, o middleware:

1. Verifica se aquela chave já foi processada (cache em memória)
2. Se sim, devolve a resposta gravada — sem tocar no banco
3. Se não, processa normalmente e grava a resposta no cache por 24h

```csharp
// src/Shared/Middleware/IdempotencyMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    // GET, HEAD, DELETE são idempotentes por natureza — ignoramos
    if (HttpMethods.IsGet(context.Request.Method) ||
        HttpMethods.IsHead(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method))
    {
        await _next(context);
        return;
    }

    if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey))
    {
        await _next(context);
        return;
    }

    var cacheKey = $"Idempotency_{idempotencyKey}";

    // Chave já processada? Devolve o resultado em cache (curto-circuito)
    if (_cache.TryGetValue(cacheKey, out CachedResponse? cached))
    {
        context.Response.StatusCode = cached!.StatusCode;
        context.Response.ContentType = cached.ContentType;
        await context.Response.WriteAsync(cached.Body ?? "");
        return;
    }

    // ... processa e grava resposta 2xx no cache por 24h
    if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
    {
        _cache.Set(cacheKey, responseToCache, TimeSpan.FromHours(24));
    }
}
```

**Como usar como cliente:**

```http
POST /api/v1/pedidos
Idempotency-Key: meu-pedido-uuid-12345
Content-Type: application/json

{ "produtoId": 1, "quantidade": 2 }
```

Se a rede falhar e você reenviar com a **mesma chave**, receberá a mesma resposta sem criar um pedido duplicado. Se enviar a **mesma chave com payload diferente**, receberá `409 Conflict`.

A trilha `docs/04-PIX.md` demonstra esse fluxo ponta a ponta em operações financeiras simuladas.

---

## 4. Segurança e JWT

Nenhum sistema moderno vive sem proteção, e a forma número #1 de protegermos APIs hoje é usando JWT.

### O que é o JWT (JSON Web Tokens)?

Imagine o JWT como uma **pulseira VIP** numa balada (sua API).

1. Você chama o endpoint de login no microserviço Auth (`POST /api/v1/auth/login`).
2. Se as credenciais estiverem corretas, o servidor gera um Token JWT com validade de 2 horas.
3. Esse token contém "Claims" (ex: seu e-mail, seu papel `Admin`).
4. O servidor **assina** o token com uma chave secreta — você não consegue forjar.
5. Para qualquer requisição protegida, você envia: `Authorization: Bearer <token>`.

### Implementação no projeto

**Geração do token** (`src/Auth/Auth.Endpoints/LoginEndpoint.cs`):

```csharp
private static IResult Login(LoginRequest req, IConfiguration configuration)
{
    if (req.Email != "admin@example.com" || req.Senha != "senha123")
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub,   "admin_id"),
        new Claim(JwtRegisteredClaimNames.Email, req.Email),
        new Claim(ClaimTypes.Role, "Admin")
    };

    var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer:            configuration["Jwt:Issuer"],
        audience:          configuration["Jwt:Audience"],
        claims:            claims,
        expires:           DateTime.UtcNow.AddHours(2),
        signingCredentials: creds
    );

    return Results.Ok(new AuthResponse
    {
        Token     = new JwtSecurityTokenHandler().WriteToken(token),
        ExpiresIn = (int)TimeSpan.FromHours(2).TotalSeconds
    });
}
```

**Validação do token** configurada no `Program.cs`:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = configuration["Jwt:Issuer"],
            ValidAudience            = configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
```

**Protegendo endpoints** — escrita exige JWT, leitura é anônima:

```csharp
group.MapGet("/", ListarProdutos)
    .AllowAnonymous()
    .RequireRateLimiting("leitura");       // público, mas com rate limit

group.MapPost("/", CriarProduto)
    .RequireAuthorization()                // exige JWT
    .RequireRateLimiting("criacao-produto"); // limite mais restritivo
```

---

## 5. Tratamento de Erros

Não responda problemas do cliente com a tela azul da morte. Use Códigos de Status e mensagens descritivas.

### Status Codes em uso neste projeto

| Família              | Código                      | Significado              | Quando retornamos                                 |
| -------------------- | --------------------------- | ------------------------ | ------------------------------------------------- |
| 2xx Sucesso          | `200 OK`                    | Tudo certo               | GET, PUT, PATCH bem-sucedidos                     |
|                      | `201 Created`               | Recurso criado           | POST que persistiu no banco                       |
|                      | `204 No Content`            | Sucesso sem corpo        | DELETE bem-sucedido                               |
| 4xx Erro do cliente  | `400 Bad Request`           | Requisição inválida      | Payload ilegível ou argumento inválido            |
|                      | `401 Unauthorized`          | Sem autenticação         | Token JWT ausente ou inválido                     |
|                      | `403 Forbidden`             | Sem autorização          | Token válido, mas sem permissão                   |
|                      | `404 Not Found`             | Recurso inexistente      | ID não existe ou produto inativo                  |
|                      | `409 Conflict`              | Conflito de estado       | Idempotency-Key reutilizada com payload diferente |
|                      | `422 Unprocessable Entity`  | Regra de negócio violada | Preço negativo, categoria inválida                |
| 5xx Erro do servidor | `500 Internal Server Error` | Falha interna            | Exceção não tratada                               |

### Exemplos de retorno nos endpoints

```csharp
// 201 Created — POST de produto bem-sucedido
return Results.Created($"/api/v1/produtos/{produto.Id}", produto);

// 204 No Content — DELETE bem-sucedido
return Results.NoContent();

// 404 Not Found — produto não encontrado
return Results.NotFound(new ErrorResponse { Title = "Produto não encontrado", Status = 404 });

// 422 Unprocessable Entity — validação falhou
return Results.UnprocessableEntity(new ErrorResponse
{
    Status = 422,
    Title  = "Validação falhou",
    Detail = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage))
});
```

### Middleware global de exceções

O `ExceptionHandlingMiddleware` em `src/Shared/Middleware/ExceptionHandlingMiddleware.cs` captura exceções não tratadas e as converte em respostas HTTP padronizadas, evitando que stack traces vazem para o cliente:

```csharp
switch (exception)
{
    case ValidationException validationEx:
        context.Response.StatusCode = 422;
        // mapeia erros de campo
        break;

    case KeyNotFoundException:
        context.Response.StatusCode = 404;
        break;

    case ArgumentException:
        context.Response.StatusCode = 400;
        break;

    case UnauthorizedAccessException:
        context.Response.StatusCode = 401;
        break;

    default:
        context.Response.StatusCode = 500;
        // detalhe interno NÃO é exposto ao cliente
        break;
}
```

---

## 6. Evolução e Versionamento

Seu Mobile App leva tempo pra atualizar e ser aprovado pela loja do Google/Apple. Se você mudar a regra do servidor e quebrar como ele funciona de repente, todos os aplicativos antigos no celular dos clientes vão **craxar simultaneamente!**

Para isso usamos _Versioning_. Este projeto utiliza versionamento por caminho de URL — a estratégia mais comum:

```csharp
// Todos os grupos de endpoints do Catálogo usam /api/v1/catalogo/ como prefixo
var catalogoGroup = app.MapGroup("/api/v1/catalogo").WithOpenApi();
var group = catalogoGroup.MapGroup("/produtos");
```

Quando uma quebra de contrato for necessária, cria-se `/api/v2/catalogo/produtos` mantendo `/api/v1/catalogo/produtos` ativo. Ambas coexistem até que todos os clientes migrem.

```
GET /api/v1/catalogo/produtos   → comportamento antigo (mantido)
GET /api/v2/catalogo/produtos   → novo contrato
```

O Swagger documenta ambas as versões no mesmo portal, facilitando a comunicação com times de front-end e mobile.

---

## 7. Performance e Resiliência

Para finalizar, APIs boas são paranoicas e precavidas.

### Caching

O projeto registra um cache em memória no `Program.cs`, utilizado pelo `IdempotencyMiddleware` para evitar reprocessamento de requisições duplicadas:

```csharp
// Program.cs
builder.Services.AddMemoryCache();
```

```csharp
// IdempotencyMiddleware — grava resposta por 24h após processamento bem-sucedido
_cache.Set(cacheKey, responseToCache, TimeSpan.FromHours(24));
```

### CORS

Browsers (Chrome, Edge) bloqueiam por padrão chamadas entre domínios diferentes. Sem configuração de CORS, um frontend em `meu-site.com` não consegue chamar a API em `minha-api.com`. Este projeto permite qualquer origem (configuração aberta para fins didáticos):

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Middleware pipeline
app.UseCors("AllowAll");
```

> Em produção, substitua `AllowAnyOrigin()` por `.WithOrigins("https://meu-frontend.com")`.

### Validação com FluentValidation

A API **não confia cegamente no front-end**. Toda entrada é validada antes de chegar ao banco. O validador de criação de produto em `src/Catalogo/Catalogo.Application/Validators/ProdutoValidator.cs` garante regras de negócio:

```csharp
public class CriarProdutoValidator : AbstractValidator<CriarProdutoRequest>
{
    private static readonly string[] CategoriasValidas =
        ["Eletrônicos", "Livros", "Roupas", "Alimentos", "Outros"];

    public CriarProdutoValidator()
    {
        RuleFor(p => p.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(100).WithMessage("Nome não pode exceder 100 caracteres");

        RuleFor(p => p.Preco)
            .GreaterThan(0).WithMessage("Preço deve ser maior que zero")
            .LessThan(999999.99m).WithMessage("Preço não pode ser tão alto");

        RuleFor(p => p.Categoria)
            .NotEmpty().WithMessage("Categoria é obrigatória")
            .Must(c => CategoriasValidas.Contains(c))
            .WithMessage("Categoria inválida. Valores aceitos: Eletrônicos, Livros, Roupas, Alimentos, Outros");

        RuleFor(p => p.Estoque)
            .GreaterThanOrEqualTo(0).WithMessage("Estoque não pode ser negativo");

        RuleFor(p => p.ContatoEmail)
            .NotEmpty().WithMessage("Email de contato é obrigatório")
            .EmailAddress().WithMessage("Email de contato inválido");
    }
}
```

Para atualizações parciais (PATCH), as regras só disparam se o campo for enviado (`.When(...)`):

```csharp
public class AtualizarProdutoValidator : AbstractValidator<AtualizarProdutoRequest>
{
    public AtualizarProdutoValidator()
    {
        RuleFor(p => p.Preco)
            .GreaterThan(0).WithMessage("Preço deve ser maior que zero")
            .When(p => p.Preco.HasValue); // só valida se o campo foi enviado

        RuleFor(p => p.ContatoEmail)
            .EmailAddress().WithMessage("Email de contato inválido")
            .When(p => !string.IsNullOrEmpty(p.ContatoEmail));
    }
}
```

Quando a validação falha, o `ExceptionHandlingMiddleware` captura a `ValidationException` do FluentValidation e retorna `422 Unprocessable Entity` com detalhes por campo — sem expor stack trace.

---

## 8. Rate Limiting

APIs públicas precisam se proteger de abuso. Sem limites, um único cliente pode saturar o servidor com requisições legítimas ou maliciosas. O .NET 8+ inclui suporte nativo a rate limiting via `Microsoft.AspNetCore.RateLimiting`.

### Por que três políticas diferentes?

Diferentes operações têm diferentes custos e frequências esperadas. Uma listagem tem impacto baixo e pode ser chamada com frequência; criar um produto tem custo alto (validação, persistência, indexação futura) e ocorre raramente.

| Política          | Algoritmo      | Limite     | Aplicação                                                    |
| ----------------- | -------------- | ---------- | ------------------------------------------------------------ |
| `leitura`         | Fixed Window   | 60 req/min | Todos os `GET`                                               |
| `escrita`         | Sliding Window | 20 req/min | `POST`, `PUT`, `PATCH`, `DELETE` (exceto criação de produto) |
| `criacao-produto` | Token Bucket   | 5 req/min  | `POST /catalogo/produtos`                                    |

### Fixed Window vs Sliding Window vs Token Bucket

**Fixed Window**: conta requisições em janelas fixas de tempo. Simples, mas permite rajadas no final/início de janelas. Adequado para leituras onde rajadas curtas são aceitáveis.

**Sliding Window**: distribui as janelas em segmentos menores, suavizando rajadas. Uma janela de 60s com 6 segmentos verifica os últimos 10s proporcionalmente. Melhor para escrita.

**Token Bucket**: o cliente acumula tokens a uma taxa constante e gasta um por requisição. Permite pequenas rajadas (até `TokenLimit`) mas sustenta apenas a taxa de reposição no longo prazo. Ideal para operações custosas com variabilidade natural.

### Implementação das três políticas

```csharp
// src/Catalogo/Catalogo.API/Extensions/RateLimitingExtensions.cs
services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Resposta ao cliente quando limite é atingido
    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();

        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            """{"erro":"Too Many Requests","mensagem":"Limite excedido. Tente novamente em breve."}""", ct);
    };

    // Política 1: leitura — janela fixa de 1 min, 60 requisições
    options.AddFixedWindowLimiter("leitura", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
        opt.QueueLimit = 0;              // sem fila — rejeita imediatamente
    });

    // Política 2: escrita — janela deslizante de 1 min, 6 segmentos, 20 requisições
    options.AddSlidingWindowLimiter("escrita", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 6;       // segmentos de 10s cada
        opt.PermitLimit = 20;
        opt.QueueLimit = 0;
    });

    // Política 3: criação de produto — token bucket, 5 tokens/min
    options.AddTokenBucketLimiter("criacao-produto", opt =>
    {
        opt.TokenLimit = 5;              // máximo de tokens acumulados
        opt.TokensPerPeriod = 5;         // tokens repostos por período
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});
```

### Aplicando políticas por endpoint

```csharp
group.MapGet("/", ListarProdutos)
    .AllowAnonymous()
    .RequireRateLimiting("leitura");           // baixo custo, alta frequência

group.MapPost("/", CriarProduto)
    .RequireAuthorization()
    .RequireRateLimiting("criacao-produto");   // custo alto, frequência baixa

group.MapPut("/{id}", Atualizar)
    .RequireAuthorization()
    .RequireRateLimiting("escrita");           // custo médio
```

### Resposta ao cliente quando limite é excedido

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 42
Content-Type: application/json

{"erro":"Too Many Requests","mensagem":"Limite excedido. Tente novamente em breve."}
```

O header `Retry-After` informa quantos segundos o cliente deve esperar antes de tentar novamente — permite que clientes bem-comportados implementem backoff automático.

### Ambiente de testes

Em `Environment = "Testing"`, as políticas de produção não são registradas. Cada factory de teste registra suas próprias políticas:

- `ApiFactory`: limites de `10000` para não interferir nos testes funcionais
- `RateLimitingApiFactory`: limites baixos (3/3/2) para testar o comportamento de rejeição

Isso evita `InvalidOperationException` por chave de política duplicada no `WebApplicationFactory`.
