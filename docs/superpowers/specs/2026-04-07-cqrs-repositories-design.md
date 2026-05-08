# Design Spec â€” CQRS Repositories (Produtos + Pedidos)

**Data:** 2026-04-07  
**Status:** Aprovado  
**Branch de implementaÃ§Ã£o:** `feature/cqrs-repositories` (worktree em `.worktrees/feature/cqrs-repositories`)

---

## Contexto

O projeto demonstra dois padrÃµes arquiteturais coexistindo: Produtos (layered) e Pedidos (vertical slices). Atualmente no `main`:

- **Produtos** usa `DapperProdutoRepository` â€” interface Ãºnica que mistura leitura e escrita, retorna entidades de domÃ­nio em todas as operaÃ§Ãµes
- **Pedidos** usa `AppDbContext` diretamente nos handlers â€” sem abstraÃ§Ã£o de repositÃ³rio

O objetivo Ã© aplicar **CQRS no nÃ­vel de repositÃ³rio** em ambas as features, separando explicitamente operaÃ§Ãµes de leitura (Dapper â†’ DTO) de operaÃ§Ãµes de escrita (EF Core â†’ entidade rastreada).

---

## Arquitetura

### PadrÃ£o adotado

Cada feature expÃµe **dois repositÃ³rios com responsabilidades distintas:**

| RepositÃ³rio | Tecnologia | Retorna | Responsabilidade |
|-------------|------------|---------|-----------------|
| `IXxxQueryRepository` | Dapper + raw SQL | DTO (`XxxResponse`) | Leituras â€” sem instanciar entidades de domÃ­nio |
| `IXxxCommandRepository` | EF Core (tracking) | Entidade de domÃ­nio | Escritas â€” carrega, muta, persiste |

### Estrutura de arquivos (feature branch)

```
src/Produtos/
â”œâ”€â”€ Produtos.Application/Repositories/
â”‚   â”œâ”€â”€ IProdutoQueryRepository.cs      # ObterPorIdAsync, ListarAsync â†’ ProdutoResponse
â”‚   â””â”€â”€ IProdutoCommandRepository.cs    # ObterPorIdAsync, AdicionarAsync, DeletarAsync, SaveChangesAsync
â””â”€â”€ Produtos.Infrastructure/Repositories/
    â”œâ”€â”€ DapperProdutoQueryRepository.cs  # Dapper, mapeia para ProdutoResponse inline (ProdutoRow)
    â””â”€â”€ EfProdutoCommandRepository.cs    # IProdutoContext (EF Core tracking)

src/Pedidos/
â”œâ”€â”€ Repositories/
â”‚   â”œâ”€â”€ IPedidoQueryRepository.cs       # ObterPorIdAsync, ListarAsync â†’ PedidoResponse
â”‚   â””â”€â”€ IPedidoCommandRepository.cs     # ObterPorIdAsync, ObterProdutoParaItemAsync, AdicionarAsync, SaveChangesAsync
â””â”€â”€ Infrastructure/
    â”œâ”€â”€ PedidoQueryRepository.cs         # Dapper, mapeia inline (PedidoRow, PedidoItemRow)
    â””â”€â”€ PedidoCommandRepository.cs       # AppDbContext (EF Core tracking)
```

---

## Fluxo de Dados

### Leitura
```
Endpoint â†’ Handler â†’ IXxxQueryRepository â†’ Dapper SQL â†’ DTO
```
- Query repo retorna `null` quando nÃ£o encontrado; endpoint converte em `404`
- Nenhuma entidade de domÃ­nio Ã© instanciada no caminho de leitura
- PaginaÃ§Ã£o via `LIMIT/OFFSET` + `COUNT(1)` separado

### Escrita
```
Endpoint â†’ Handler/Service â†’ IXxxCommandRepository.ObterPorIdAsync â†’ [mutaÃ§Ã£o no domÃ­nio] â†’ SaveChangesAsync
```
- Command repo carrega entidade **rastreada** pelo EF Core
- Handler aplica mutaÃ§Ãµes via domain methods (`Desativar()`, `AdicionarItem()`, etc.)
- EF detecta mudanÃ§as automaticamente via change tracking
- `SaveChangesAsync` Ã© chamado pelo handler (nÃ£o pelo repo) â€” exceto em `AdicionarAsync` de Produtos, que persiste internamente

### Caso especial â€” AddItemPedido
`IPedidoCommandRepository` expÃµe `ObterProdutoParaItemAsync(produtoId)` porque `pedido.AdicionarItem(produto, quantidade)` precisa da entidade `Produto` rastreada. Isso mantÃ©m a lÃ³gica de domÃ­nio intacta sem vazar `AppDbContext` para fora da infraestrutura.

---

## Camada de ServiÃ§o

**Produtos:** `ProdutoService` mantÃ©m-se como orquestrador, agora injetando ambos os repositÃ³rios:
```csharp
public ProdutoService(
    IProdutoQueryRepository queryRepo,
    IProdutoCommandRepository commandRepo,
    IMapper mapper,
    ILogger<ProdutoService> logger)
```

**Pedidos:** handlers de cada slice injetam o repositÃ³rio diretamente â€” sem service layer (padrÃ£o vertical slice mantido).

---

## Registro de DependÃªncias

**Produtos** (`ProdutosServiceExtensions.cs`):
```csharp
services.AddScoped<IProdutoService, ProdutoService>();
services.AddScoped<IProdutoQueryRepository, DapperProdutoQueryRepository>();
services.AddScoped<IProdutoCommandRepository, EfProdutoCommandRepository>();
```

**Pedidos** (`Program.cs`):
```csharp
services.AddScoped<IPedidoQueryRepository, PedidoQueryRepository>();
services.AddScoped<IPedidoCommandRepository, PedidoCommandRepository>();
```

---

## EstratÃ©gia de ValidaÃ§Ã£o

1. `dotnet build` na worktree â€” zero erros
2. `dotnet test` â€” todos os testes devem passar:
   - `FacShopAPI.Tests` â€” 23+ testes HTTP (endpoints, auth, paginaÃ§Ã£o, soft delete)
   - `Pedidos.Tests` â€” testes de todos os slices (Create, Get, List, AddItem, Cancel)
   - Testes unitÃ¡rios de domÃ­nio (value objects, FSM de Pedido)
3. Corrigir qualquer falha antes do merge

---

## O que NÃƒO muda

- Endpoints (Produtos e Pedidos)
- Validators (FluentValidation)
- Domain model (Produto, Pedido, value objects)
- Migrations e AppDbContext
- Middleware (Idempotency, ExceptionHandling)
- IEndpoint auto-registration pattern

---

## Plano de IntegraÃ§Ã£o

- Abordagem: merge direto (sem squash) de `feature/cqrs-repositories` â†’ `main`
- Criar PR para checkpoint de revisÃ£o
- ApÃ³s merge aprovado, remover worktree
