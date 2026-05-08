# POC Arquitetura Backend Faciltech â€” Design Spec

**Data:** 2026-04-17  
**Decisor:** Marco Mendes  
**Status:** Aprovado

---

## Contexto

Projeto educacional (`net-minimal-api`) demonstrando padrÃµes arquiteturais coexistindo. Esta POC expande o projeto para cobrir:

1. **MigraÃ§Ã£o Produtos â†’ CatÃ¡logo** com arquitetura hÃ­brida (Clean Architecture nas camadas + Vertical Slice na API)
2. **Recursos de domÃ­nio rico seletivo** â€” Categorias e Variantes com agregados, Atributos e MÃ­dias como CRUD simples
3. **Tratamento de erro 429** â€” rate limiting no servidor + resiliÃªncia no client
4. **Versionamento manual de API** via route groups
5. **ADRs padronizadas** com template MADR, ciclo de vida e novas decisÃµes

---

## Fase 1 â€” FundaÃ§Ã£o: MigraÃ§Ã£o Produtos â†’ CatÃ¡logo

### Roteamento e Versionamento

A API de Produtos deixa de existir como conceito isolado. Tudo migra para o bounded context **CatÃ¡logo** com versionamento manual via route groups aninhados:

```
/api/v1/catalogo/produtos
/api/v1/catalogo/categorias
/api/v1/catalogo/variantes
/api/v1/catalogo/atributos
/api/v1/catalogo/midias
```

```csharp
var v1 = app.MapGroup("/api/v1");
var catalogo = v1.MapGroup("/catalogo");

catalogo.MapProdutoEndpoints();
catalogo.MapCategoriaEndpoints();
catalogo.MapVarianteEndpoints();
catalogo.MapAtributoEndpoints();
catalogo.MapMidiaEndpoints();
```

Pedidos continua em `/api/v1/pedidos`. Auth em `/api/v1/auth`. Ambos intocados.

### Arquitetura Interna â€” HÃ­brida

Clean Architecture nas camadas Domain/Application/Infrastructure. Vertical Slice na camada API (um arquivo por endpoint).

```
src/Catalogo/
â”œâ”€â”€ Catalogo.Domain/
â”‚   â”œâ”€â”€ Entities/
â”‚   â”‚   â”œâ”€â”€ Produto.cs
â”‚   â”‚   â”œâ”€â”€ Categoria.cs
â”‚   â”‚   â”œâ”€â”€ Variante.cs
â”‚   â”‚   â”œâ”€â”€ Atributo.cs
â”‚   â”‚   â””â”€â”€ Midia.cs
â”‚   â”œâ”€â”€ ValueObjects/
â”‚   â”‚   â”œâ”€â”€ Preco.cs
â”‚   â”‚   â”œâ”€â”€ Estoque.cs
â”‚   â”‚   â”œâ”€â”€ SKU.cs
â”‚   â”‚   â””â”€â”€ UrlMidia.cs
â”‚   â””â”€â”€ Common/
â”‚       â””â”€â”€ Result.cs
â”‚
â”œâ”€â”€ Catalogo.Application/
â”‚   â”œâ”€â”€ DTOs/
â”‚   â”‚   â”œâ”€â”€ Produto/
â”‚   â”‚   â”œâ”€â”€ Categoria/
â”‚   â”‚   â”œâ”€â”€ Variante/
â”‚   â”‚   â”œâ”€â”€ Atributo/
â”‚   â”‚   â””â”€â”€ Midia/
â”‚   â”œâ”€â”€ Interfaces/
â”‚   â”‚   â””â”€â”€ ICatalogoContext.cs
â”‚   â”œâ”€â”€ Repositories/
â”‚   â”‚   â”œâ”€â”€ IProdutoRepository.cs
â”‚   â”‚   â”œâ”€â”€ ICategoriaRepository.cs
â”‚   â”‚   â”œâ”€â”€ IVarianteRepository.cs
â”‚   â”‚   â”œâ”€â”€ IAtributoRepository.cs
â”‚   â”‚   â””â”€â”€ IMidiaRepository.cs
â”‚   â”œâ”€â”€ Services/
â”‚   â”‚   â”œâ”€â”€ ProdutoService.cs
â”‚   â”‚   â”œâ”€â”€ CategoriaService.cs
â”‚   â”‚   â”œâ”€â”€ VarianteService.cs
â”‚   â”‚   â”œâ”€â”€ AtributoService.cs
â”‚   â”‚   â””â”€â”€ MidiaService.cs
â”‚   â”œâ”€â”€ Validators/
â”‚   â””â”€â”€ Mappings/
â”‚
â”œâ”€â”€ Catalogo.Infrastructure/
â”‚   â”œâ”€â”€ Repositories/       â† EF Core (CQRS write)
â”‚   â”œâ”€â”€ Queries/            â† Dapper (CQRS read)
â”‚   â””â”€â”€ Data/
â”‚       â””â”€â”€ DbSeeder.cs
â”‚
â””â”€â”€ Catalogo.API/
    â”œâ”€â”€ Endpoints/
    â”‚   â”œâ”€â”€ Produtos/       â† vertical slice por recurso
    â”‚   â”œâ”€â”€ Categorias/
    â”‚   â”œâ”€â”€ Variantes/
    â”‚   â”œâ”€â”€ Atributos/
    â”‚   â””â”€â”€ Midias/
    â””â”€â”€ Extensions/
        â””â”€â”€ CatalogoServiceExtensions.cs
```

### Mapa de MudanÃ§as

| Antes | Depois |
|-------|--------|
| `src/Produtos/` (flat) | `src/Catalogo/` (4 sub-projetos) |
| `FacShopAPI.Produtos.*` | `FacShopAPI.Catalogo.*` |
| `/api/v1/produtos` | `/api/v1/catalogo/produtos` |
| `IProdutoContext` | `ICatalogoContext` |
| `docs/plans/2026-03-02-produtos-clean-architecture.md` | Superseded â€” absorvido por esta migraÃ§Ã£o |

`AppDbContext` permanece em `src/Shared/Data/` e implementa `ICatalogoContext`. Pedidos, Shared e Pix: intocados.

---

## Fase 2 â€” DomÃ­nio: Novos Recursos do CatÃ¡logo

### Categorias â€” DomÃ­nio Rico com Hierarquia de Dois NÃ­veis

```csharp
public class Categoria
{
    public int Id { get; private set; }
    public string Nome { get; private set; }
    public string Slug { get; private set; }       // gerado automaticamente
    public int? CategoriaPaiId { get; private set; }
    public bool Ativa { get; private set; }

    public static Result<Categoria> Criar(string nome, int? categoriaPaiId = null) { ... }
    public Result Renomear(string novoNome) { ... }
    public Result Desativar() { ... }
}
```

**Regras de domÃ­nio:**
- Slug gerado a partir do nome (lowercase, sem acentos, hÃ­fens no lugar de espaÃ§os)
- Categoria filha nÃ£o pode ter filhas (mÃ¡ximo dois nÃ­veis â€” validado no domÃ­nio)
- NÃ£o Ã© possÃ­vel desativar categoria com produtos ativos vinculados

**Endpoints** (`/api/v1/catalogo/categorias`):

| MÃ©todo | Rota | DescriÃ§Ã£o |
|--------|------|-----------|
| GET | `/` | Lista categorias raiz com subcategorias aninhadas |
| GET | `/{id}` | ObtÃ©m categoria com subcategorias |
| POST | `/` | Cria categoria raiz ou subcategoria (`categoriaPaiId` opcional) |
| PUT | `/{id}` | Renomeia categoria |
| DELETE | `/{id}` | Desativa categoria (valida produtos ativos) |

### Variantes â€” DomÃ­nio Rico com SKU

```csharp
public class Variante
{
    public int Id { get; private set; }
    public int ProdutoId { get; private set; }
    public SKU Sku { get; private set; }
    public string Descricao { get; private set; }
    public Preco PrecoAdicional { get; private set; }
    public Estoque Estoque { get; private set; }
    public bool Ativa { get; private set; }

    public static Result<Variante> Criar(int produtoId, string sku, string descricao,
                                         decimal precoAdicional, int estoque) { ... }
    public Result AtualizarEstoque(int quantidade) { ... }
    public Result AtualizarPreco(decimal novoPrecoAdicional) { ... }
    public Result Desativar() { ... }
}
```

**Value Object SKU:**
- 6â€“20 caracteres
- Apenas letras maiÃºsculas, nÃºmeros e hÃ­fens (`^[A-Z0-9\-]+$`)
- Ãšnico por produto (validado na camada Application)

**Endpoints** (`/api/v1/catalogo/variantes`):

| MÃ©todo | Rota | DescriÃ§Ã£o |
|--------|------|-----------|
| GET | `/?produtoId={id}` | Lista variantes de um produto |
| GET | `/{id}` | ObtÃ©m variante especÃ­fica |
| POST | `/` | Cria variante (`produtoId` no body) |
| PUT | `/{id}` | Atualiza preÃ§o adicional |
| PATCH | `/{id}/estoque` | Atualiza estoque da variante |
| DELETE | `/{id}` | Desativa variante |

### Atributos e MÃ­dias â€” CRUD Simples

**Atributo:** chave-valor associado a produto (`"Cor": "Azul"`, `"Material": "AlgodÃ£o"`). Sem regras de domÃ­nio complexas. CRUD completo em `/api/v1/catalogo/atributos`.

**MÃ­dia:** URL externa associada ao produto, com tipo (`Imagem`, `Video`, `Documento`) e ordem de exibiÃ§Ã£o. Sem upload de arquivo â€” apenas referÃªncia de URL. CRUD completo em `/api/v1/catalogo/midias`.

Ambos demonstram que dentro do mesmo bounded context coexistem recursos de complexidade diferente â€” decisÃ£o documentada em ADR-0015.

---

## Fase 3 â€” ResiliÃªncia: Tratamento de Erro 429

### Rate Limiting no Servidor

Usar `Microsoft.AspNetCore.RateLimiting` (built-in .NET). TrÃªs polÃ­ticas intencionalmente diferentes para fins educacionais:

| PolÃ­tica | Onde aplica | Regra | Justificativa didÃ¡tica |
|----------|------------|-------|----------------------|
| `fixed-window` | GET `/catalogo/*` | 60 req/min por IP | Leitura pÃºblica, janela fixa simples |
| `sliding-window` | POST/PUT/PATCH/DELETE `/catalogo/*` | 20 req/min por IP | Escrita â€” janela deslizante mais justa |
| `token-bucket` | POST `/catalogo/produtos` | 5 tokens, repÃµe 1/min | CriaÃ§Ã£o cara â€” bucket demonstra burst |

Resposta 429 inclui header `Retry-After` com segundos atÃ© o prÃ³ximo slot.

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", cancellationToken: token);
    };
    // polÃ­ticas configuradas aqui
});
```

Middleware adicionado ao pipeline antes do roteamento. Endpoints declaram polÃ­tica com `.RequireRateLimiting("policy-name")`.

### ResiliÃªncia no Client

Novo projeto `src/Catalogo/Catalogo.ClientDemo/` seguindo o padrÃ£o jÃ¡ existente em `Pix.ClientDemo`.

**Pipeline de resiliÃªncia:**

```csharp
builder.Services
    .AddHttpClient<CatalogoHttpClient>(client =>
        client.BaseAddress = new Uri("http://localhost:5000"))
    .AddResilienceHandler("catalogo", pipeline =>
    {
        // 1. Retry com backoff exponencial â€” trata 429 e 5xx, respeita Retry-After
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = args => args.Outcome switch
            {
                { Result.StatusCode: HttpStatusCode.TooManyRequests } => PredicateResult.True(),
                { Result.StatusCode: HttpStatusCode.InternalServerError } => PredicateResult.True(),
                _ => PredicateResult.False()
            }
        });

        // 2. Circuit Breaker â€” abre apÃ³s falhas consecutivas
        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.5,
            MinimumThroughput = 3,
            BreakDuration = TimeSpan.FromSeconds(30)
        });

        // 3. Timeout por requisiÃ§Ã£o
        pipeline.AddTimeout(TimeSpan.FromSeconds(10));
    });
```

O demo dispara 10 requisiÃ§Ãµes sequenciais, exibe os 429 recebidos, os retries automÃ¡ticos com backoff, e o circuit breaker abrindo.

---

## Fase 4 â€” DocumentaÃ§Ã£o: ADRs

### Template MADR Padronizado

Todas as ADRs (existentes e novas) adotam o formato MADR 3.x:

```markdown
---
status: accepted
date: YYYY-MM-DD
deciders: [Marco Mendes]
superseded-by:
---

# NNNNN â€” TÃ­tulo

## Contexto e Problema
## DecisÃ£o
## ConsequÃªncias
### Positivas
### Negativas / Trade-offs
## Alternativas Consideradas
```

### ADRs Existentes (0001â€“0010)

Todas recebem `date` e `deciders`. Status permanece `accepted`. Nenhuma Ã© deprecated â€” o plano `2026-03-02-produtos-clean-architecture.md` Ã© marcado como superseded internamente pelas novas ADRs, nÃ£o nas ADRs antigas.

### Novas ADRs

| ADR | TÃ­tulo | Status |
|-----|--------|--------|
| 0011 | Arquitetura HÃ­brida: Clean Architecture + Vertical Slices na API | `accepted` |
| 0012 | Versionamento manual de API via route groups | `accepted` |
| 0013 | Rate Limiting com AspNetCore.RateLimiting â€” trÃªs polÃ­ticas | `accepted` |
| 0014 | ResiliÃªncia no client HTTP com HttpResilienceHandler | `accepted` |
| 0015 | DomÃ­nio Rico Seletivo dentro do mesmo bounded context | `accepted` |

---

## Testes

### Estrutura (dentro de `FacShopAPI.Tests/`)

```
Unit/
â”œâ”€â”€ Domain/
â”‚   â”œâ”€â”€ ProdutoTests.cs          â† namespaces atualizados
â”‚   â”œâ”€â”€ CategoriaTests.cs
â”‚   â””â”€â”€ VarianteTests.cs
â””â”€â”€ ValueObjects/
    â”œâ”€â”€ SKUTests.cs
    â””â”€â”€ UrlMidiaTests.cs

Integration/
â”œâ”€â”€ Catalogo/
â”‚   â”œâ”€â”€ ProdutoEndpointsTests.cs â† rotas atualizadas
â”‚   â”œâ”€â”€ CategoriaEndpointsTests.cs
â”‚   â”œâ”€â”€ VarianteEndpointsTests.cs
â”‚   â”œâ”€â”€ AtributoEndpointsTests.cs
â”‚   â””â”€â”€ MidiaEndpointsTests.cs
â””â”€â”€ RateLimiting/
    â””â”€â”€ RateLimitingTests.cs
```

### ConvenÃ§Ã£o de IDs no DbSeeder

| Entidade | IDs Reservados | Testes criam a partir de |
|----------|---------------|--------------------------|
| Produtos | 1â€“8 | ID 9+ |
| Categorias | 1â€“5 | ID 6+ |
| Variantes | 1â€“3 | ID 4+ |
| Atributos | 1â€“4 | ID 5+ |
| MÃ­dias | 1â€“2 | ID 3+ |

### Cobertura de Rate Limiting

- Dispara limite+1 requests, verifica que a Ãºltima retorna 429
- Verifica presenÃ§a e valor numÃ©rico do header `Retry-After`
- Verifica que POST atinge 429 antes do GET no mesmo intervalo de tempo

### O que nÃ£o muda

- `Pedidos.Tests/` â€” intocado
- `AuthHelper.ObterTokenAsync(client)` â€” mesma autenticaÃ§Ã£o nos testes
- `WebApplicationFactory` â€” mesma factory base, seeder expandido
- Ambiente `Testing` com SQLite in-memory

---

## Abordagem de ExecuÃ§Ã£o

**Abordagem A â€” Big Bang Coordenado** em 4 fases sequenciais:

1. **Fase 1 â€” FundaÃ§Ã£o:** Migrar Produtos â†’ CatÃ¡logo com Clean Architecture + renomear rotas para `/api/v1/catalogo/*`
2. **Fase 2 â€” DomÃ­nio:** Adicionar Categorias, Variantes, Atributos e MÃ­dias
3. **Fase 3 â€” ResiliÃªncia:** Rate limiting no servidor + `Catalogo.ClientDemo` com Polly
4. **Fase 4 â€” DocumentaÃ§Ã£o:** Template MADR, atualizar 10 ADRs existentes, criar 5 novas ADRs

Cada fase Ã© um marco testÃ¡vel â€” testes devem passar integralmente ao final de cada fase antes de avanÃ§ar.
