# Design: Vertical Slice Architecture + DomÃ­nio Rico (Pedidos)

**Data:** 2026-02-27
**Status:** Aprovado

---

## Contexto

O projeto FacShopAPI Ã© um projeto educacional em .NET 10 Minimal API com arquitetura horizontal em camadas (Endpoints â†’ Services â†’ Data). O modelo de domÃ­nio atual Ã© anÃªmico: a classe `Produto` Ã© um contÃªiner de dados sem comportamento.

Este design introduz:
1. **Vertical Slice Architecture** para o novo caso de uso de Pedidos
2. **Modelo de domÃ­nio rico** em Pedidos e na refatoraÃ§Ã£o de Produtos
3. CoexistÃªncia dos dois padrÃµes no mesmo projeto, demonstrando o contraste

---

## DecisÃµes de Design

| DecisÃ£o | Escolha | Motivo |
|---|---|---|
| Escopo de Pedidos | Pedido + PedidoItem | Agregado clÃ¡ssico, didÃ¡tico |
| OrganizaÃ§Ã£o dos slices | CRUD como slices | AcessÃ­vel, sem overhead de casos de uso complexos |
| Handler | Inline no Command, sem MediatR | Sem dependÃªncias novas, mais simples |
| PersistÃªncia | Mesmo AppDbContext | Evita complexidade de mÃºltiplos contextos |
| Produtos | Refatorado com domÃ­nio rico | Demonstra que domÃ­nio rico independe do padrÃ£o arquitetural |

---

## Estrutura de Pastas

```
src/
â”œâ”€â”€ Endpoints/              â† Produtos (inalterado â€” horizontal layers)
â”‚   â”œâ”€â”€ ProdutoEndpoints.cs
â”‚   â””â”€â”€ AuthEndpoints.cs
â”œâ”€â”€ Services/               â† Produtos (inalterado)
â”œâ”€â”€ Models/                 â† Produto (refatorado com domÃ­nio rico)
â”œâ”€â”€ DTOs/                   â† Produtos
â”œâ”€â”€ Validators/             â† Produtos
â”‚
â””â”€â”€ Features/               â† NOVO â€” Vertical Slice
    â””â”€â”€ Pedidos/
        â”œâ”€â”€ Domain/
        â”‚   â”œâ”€â”€ Pedido.cs
        â”‚   â”œâ”€â”€ PedidoItem.cs
        â”‚   â”œâ”€â”€ StatusPedido.cs
        â”‚   â””â”€â”€ PedidoErrors.cs
        â”œâ”€â”€ Common/
        â”‚   â”œâ”€â”€ IEndpoint.cs
        â”‚   â”œâ”€â”€ Result.cs
        â”‚   â””â”€â”€ PedidoResponse.cs
        â”œâ”€â”€ CreatePedido/
        â”‚   â”œâ”€â”€ CreatePedidoCommand.cs
        â”‚   â”œâ”€â”€ CreatePedidoValidator.cs
        â”‚   â””â”€â”€ CreatePedidoEndpoint.cs
        â”œâ”€â”€ GetPedido/
        â”‚   â”œâ”€â”€ GetPedidoQuery.cs
        â”‚   â””â”€â”€ GetPedidoEndpoint.cs
        â”œâ”€â”€ ListPedidos/
        â”‚   â”œâ”€â”€ ListPedidosQuery.cs
        â”‚   â””â”€â”€ ListPedidosEndpoint.cs
        â”œâ”€â”€ AddItemPedido/
        â”‚   â”œâ”€â”€ AddItemCommand.cs
        â”‚   â”œâ”€â”€ AddItemValidator.cs
        â”‚   â””â”€â”€ AddItemEndpoint.cs
        â””â”€â”€ CancelPedido/
            â”œâ”€â”€ CancelPedidoCommand.cs
            â””â”€â”€ CancelPedidoEndpoint.cs
```

---

## Modelo de DomÃ­nio Rico

### Pedido (Aggregate Root)

Regras de negÃ³cio encapsuladas:

- Itens sÃ³ podem ser adicionados a pedidos em status `Rascunho`
- Quantidade por item: mÃ­nimo 1, mÃ¡ximo 999
- Mesmo produto adicionado duas vezes faz merge de quantidade
- Limite de 20 itens distintos por pedido
- Produto precisa estar ativo e com estoque suficiente
- Confirmar requer ao menos 1 item e valor mÃ­nimo de R$ 10,00
- Pedido `Confirmado` ou `Cancelado` nÃ£o pode ser confirmado novamente
- Cancelamento exige motivo obrigatÃ³rio
- Pedido jÃ¡ cancelado nÃ£o pode ser cancelado novamente

### PedidoItem (Entity filha)

- PreÃ§o unitÃ¡rio e nome do produto sÃ£o **snapshots** do momento do pedido
- `Subtotal` Ã© calculado (`PrecoUnitario * Quantidade`), nunca persistido
- Construtores e mutaÃ§Ãµes com acesso `internal` â€” protegidos fora do agregado

### Produto (refatorado)

MÃ©todos de domÃ­nio substituem setters pÃºblicos:

- `Criar(...)` â€” factory method com validaÃ§Ã£o (retorna `Result<Produto>`)
- `AtualizarPreco(decimal)` â€” valida que Ã© > 0 e diferente do atual
- `ReporEstoque(int)` â€” valida positivo e nÃ£o excede 99.999 unidades
- `Desativar()` â€” valida que nÃ£o estÃ¡ jÃ¡ inativo
- `TemEstoqueDisponivel(int)` â€” guard usado pelo agregado Pedido

### Result Pattern

```csharp
public record Result(bool IsSuccess, string? Error = null)
public record Result<T>(bool IsSuccess, T? Value, string? Error = null)
```

Erros de domÃ­nio retornam via `Result` â€” sem exceptions para fluxo de negÃ³cio.

---

## Arquitetura dos Slices

### Interface IEndpoint â€” registro automÃ¡tico

```csharp
public interface IEndpoint
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
```

`Program.cs` faz scan de `IEndpoint` via `AddEndpointsFromAssembly` â€” nenhum slice Ã© registrado manualmente.

### Anatomia de um slice

Cada slice tem trÃªs responsabilidades separadas:

1. **Command/Query** â€” DTO de entrada + Handler com lÃ³gica de aplicaÃ§Ã£o
2. **Validator** â€” FluentValidation do DTO (quando necessÃ¡rio)
3. **Endpoint** â€” apenas roteamento HTTP, sem lÃ³gica

### CoexistÃªncia dos padrÃµes

```
Produtos (Horizontal Layers)          Pedidos (Vertical Slice)
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€         â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
ProdutoEndpoints.cs                   Features/Pedidos/CreatePedido/
  â””â”€ chama IProdutoService              â””â”€ CreatePedidoEndpoint.cs
       â””â”€ usa AppDbContext               â””â”€ CreatePedidoHandler
                                         â””â”€ AppDbContext (mesmo)
```

Ambos usam o mesmo `AppDbContext`, mesma pipeline JWT, mesmo middleware.

---

## Recursos .NET 10

| Feature | Onde aparece |
|---|---|
| `TypedResults` | Todos os endpoints dos slices |
| `MapGroup` com metadados herdados | Grupo `/api/v1/pedidos` centraliza auth + tag |
| Primary constructors | Handlers dos slices |
| `IEndpoint` scan automÃ¡tico | `AddEndpointsFromAssembly` |
| `Results.ValidationProblem` (RFC 7807) | FluentValidation nos slices |
| Collection expressions `[]` | `_itens = []` no aggregate |

---

## EstratÃ©gia de Testes

### Testes de DomÃ­nio (Unit) â€” sem infraestrutura

- `PedidoTests.cs` â€” todas as regras do aggregate: merge de itens, limite de 20, valor mÃ­nimo, cancelamento, etc.
- `PedidoItemTests.cs` â€” snapshot de preÃ§o, incremento de quantidade
- `ProdutoTests.cs` â€” regras do domÃ­nio rico refatorado

### Testes de IntegraÃ§Ã£o (HTTP)

- `CreatePedidoTests.cs`, `GetPedidoTests.cs`, `AddItemTests.cs`, `CancelPedidoTests.cs`
- Via `WebApplicationFactory<Program>`, ponta a ponta com SQLite in-memory

### ProdutoBuilder (Test Helper)

Builder fluente para criaÃ§Ã£o de `Produto` em testes de domÃ­nio, sem boilerplate.

---

## Endpoints resultantes

| MÃ©todo | Rota | Slice | Auth |
|---|---|---|---|
| `POST` | `/api/v1/pedidos` | CreatePedido | Sim |
| `GET` | `/api/v1/pedidos/{id}` | GetPedido | Sim |
| `GET` | `/api/v1/pedidos` | ListPedidos | Sim |
| `POST` | `/api/v1/pedidos/{id}/itens` | AddItemPedido | Sim |
| `POST` | `/api/v1/pedidos/{id}/cancelar` | CancelPedido | Sim |
