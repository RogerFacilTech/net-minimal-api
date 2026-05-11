# Pedidos â€” Vertical Slice e DomÃ­nio Rico

## Autenticacao no contexto da solution

O servico de `Pedidos` e um resource server.

- Nao emite token JWT.
- Exige token JWT nos endpoints protegidos.
- Valida token com `JwtBearer` (assinatura, issuer, audience, exp).
- O emissor oficial de token e o microservico `Auth` (`POST /api/v1/auth/login`).

> Complemento didÃ¡tico: para integraÃ§Ã£o externa com APIs e JSON complexo, veja [04-PIX.md](04-PIX.md), que cobre `HttpClientFactory`, idempotÃªncia e servidor mock auto-contido.

Para entender a arquitetura do Catálogo (CA híbrida em camadas), explore `src/Catalogo/Catalogo.API/Endpoints/`.

---

## 1. O Problema com Camadas Horizontais

Arquiteturas tradicionais em camadas (Endpoints â†’ Services â†’ Data) funcionam bem atÃ© um ponto. Uma mudanÃ§a no domÃ­nio exige ediÃ§Ãµes em mÃºltiplos lugares:

> **Exemplo:** Adicionar um novo campo `Desconto` ao CatÃ¡logo exigiria tocar em:
>
> 1. `Produto.cs` â€” adicionar propriedade
> 2. `CriarProdutoValidator.cs` â€” adicionar regra
> 3. `AtualizarProdutoValidator.cs` â€” idem
> 4. `ProdutoDTO.cs` â€” adicionar Request/Response
> 5. `MappingProfile.cs` â€” adicionar mapping
> 6. `AppDbContext.cs` â€” configurar
> 7. Database â€” executar migration

Essa dispersÃ£o acontece porque o domÃ­nio Ã© **anÃªmico** â€” entidades sÃ£o apenas contÃªineres de dados, e toda a lÃ³gica vive em serviÃ§os genÃ©ricos.

---

## 2. Vertical Slice Architecture

### O que Ã©?

Uma **slice** (fatia) representa **um Ãºnico caso de uso** ou funcionalidade. Todas as peÃ§as necessÃ¡rias para executÃ¡-la residem em uma pasta isolada:

```
src/Pedidos/Pedidos.API/CreatePedido/
  ├─ CreatePedidoCommand.cs     # DTO de entrada + Handler (orquestração)
  ├─ CreatePedidoValidator.cs   # FluentValidation
  └─ CreatePedidoEndpoint.cs    # Rota HTTP, implementa IEndpoint
```

Cada slice Ã© **independente**: alterar o comportamento de criaÃ§Ã£o de pedido nÃ£o afeta diretamente outras operaÃ§Ãµes.

### BenefÃ­cios

| BenefÃ­cio         | DescriÃ§Ã£o                                                |
| ------------------ | ---------------------------------------------------------- |
| **CoesÃ£o Alta**   | Tudo para fazer uma tarefa estÃ¡ num lugar                 |
| **IndependÃªncia** | Cada slice pode evoluir isoladamente                       |
| **Escalabilidade** | FÃ¡cil adicionar novos casos de uso                        |
| **Onboarding**     | Novo dev consegue entender um caso de uso completo rÃ¡pido |
| **Low Coupling**   | Mexer em uma slice nÃ£o quebra outras                      |

### Anatomia de um Slice (exemplo: CreatePedido)

#### 2.1 Command (DTO de entrada)

```csharp
public sealed record CreatePedidoCommand(
    string ClienteNome,
    List<AddItemCommand> Itens
);

public sealed record AddItemCommand(
    int ProdutoId,
    int Quantidade
);
```

#### 2.2 Validator (FluentValidation)

```csharp
public sealed class CreatePedidoValidator : AbstractValidator<CreatePedidoCommand>
{
    public CreatePedidoValidator()
    {
        RuleFor(x => x.ClienteNome)
            .NotEmpty().WithMessage("Nome do cliente Ã© obrigatÃ³rio")
            .Length(3, 100);

        RuleForEach(x => x.Itens)
            .NotNull()
            .DependentRules(() =>
            {
                RuleFor(x => x.ProdutoId).GreaterThan(0);
                RuleFor(x => x.Quantidade).GreaterThan(0);
            });
    }
}
```

#### 2.3 Handler (OrquestraÃ§Ã£o com domÃ­nio)

```csharp
public record CreatePedidoCommand(List<CreatePedidoItemDto> Itens);
public record CreatePedidoItemDto(int ProdutoId, int Quantidade);

public class CreatePedidoHandler(IPedidoCommandRepository repository)
{
    public async Task<Result<PedidoResponse>> HandleAsync(
        CreatePedidoCommand cmd, CancellationToken ct = default)
    {
        var pedido = Pedido.Criar();

        foreach (var itemDto in cmd.Itens)
        {
            var produto = await repository.ObterProdutoParaItemAsync(itemDto.ProdutoId, ct);
            if (produto is null)
                return Result<PedidoResponse>.Fail($"Produto {itemDto.ProdutoId} não encontrado.");

            var resultado = pedido.AdicionarItem(produto, itemDto.Quantidade);
            if (!resultado.IsSuccess)
                return Result<PedidoResponse>.Fail(resultado.Error!);
        }

        await repository.AdicionarAsync(pedido, ct);
        await repository.SaveChangesAsync(ct);

        return Result<PedidoResponse>.Ok(PedidoResponse.From(pedido));
    }
}
```

#### 2.4 Endpoint (Rota HTTP)

```csharp
public sealed class CreatePedidoEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app) =>
        app.MapPost("/api/v1/pedidos", async (
            CreatePedidoCommand cmd,
            CreatePedidoHandler handler,
            IValidator<CreatePedidoCommand> validator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(cmd, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(cmd, ct);
            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });

            return Results.Created($"/api/v1/pedidos/{result.Value!.Id}", result.Value);
        })
        .RequireAuthorization()
        .WithName("CreatePedido")
        .WithOpenApi();
}
```

---

## 3. IEndpoint e Auto-Discovery

**O desafio:** Em Vertical Slice, cada slice tem seu prÃ³prio endpoint. RegistrÃ¡-los manualmente seria tedioso.

**A soluÃ§Ã£o:** Interface comum `IEndpoint` + descoberta via reflexÃ£o.

```csharp
// src/Shared/Web/IEndpoint.cs
public interface IEndpoint
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
```

No `Program.cs`:

```csharp
builder.Services.AddEndpointsFromAssembly(typeof(CreatePedidoEndpoint).Assembly);
app.MapRegisteredEndpoints();
```

Isso varre todos os tipos implementando `IEndpoint` e chama `.MapEndpoints()` automaticamente. Basta criar `NovoSliceEndpoint : IEndpoint` e ela serÃ¡ descoberta â€” sem cadastro manual.

---

## 4. Modelo AnÃªmico vs DomÃ­nio Rico

### Produto HipotÃ©tico (AnÃªmico)

O exemplo abaixo mostra como seria um `Produto` puramente anÃªmico â€” sem regras encapsuladas:

```csharp
public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public bool Ativo { get; set; }
    // Nenhuma regra de negÃ³cio encapsulada aqui!
}
```

**CaracterÃ­sticas:**

- Apenas propriedades (get/set)
- Sem mÃ©todos de negÃ³cio
- ValidaÃ§Ãµes em `ProdutoValidator`
- LÃ³gica em `ProdutoService`

**Onde as regras vivem:**

- "PreÃ§o nÃ£o pode ser negativo" â†’ `ProdutoValidator`
- "NÃ£o pode vender fora do estoque" â†’ `ProdutoService`
- "Ativo garante disponibilidade" â†’ `ProdutoService`

### Pedido (Rico) â€” Vertical Slice

```csharp
public sealed class Pedido
{
    private readonly List<PedidoItem> _itens = new();

    public int Id { get; set; }
    public string ClienteNome { get; set; }
    public PedidoStatus Status { get; set; }

    // Propriedade calculada!
    public decimal Total => _itens.Sum(i => i.Total);

    // Regras encapsuladas em mÃ©todos:

    public static Result<Pedido> Create(string clienteNome)
    {
        if (string.IsNullOrWhiteSpace(clienteNome))
            return Result<Pedido>.Fail("Nome do cliente obrigatÃ³rio");

        if (clienteNome.Length > 100)
            return Result<Pedido>.Fail("Nome muito longo");

        return Result<Pedido>.Ok(new Pedido
        {
            ClienteNome = clienteNome,
            Status = PedidoStatus.Aberto,
            DataCriacao = DateTime.Now
        });
    }

    public Result AddItem(Produto produto, int quantidade)
    {
        if (Status != PedidoStatus.Aberto)
            return Result.Fail("Pedido nÃ£o estÃ¡ aberto");

        if (quantidade <= 0)
            return Result.Fail("Quantidade deve ser positiva");

        if (produto.Estoque < quantidade)
            return Result.Fail("Estoque insuficiente");

        _itens.Add(new PedidoItem(produto, quantidade));
        return Result.Ok();
    }

    public Result Cancel()
    {
        if (Status != PedidoStatus.Aberto)
            return Result.Fail("SÃ³ pedidos abertos podem ser cancelados");

        Status = PedidoStatus.Cancelado;
        return Result.Ok();
    }
}
```

**CaracterÃ­sticas:**

- Propriedades + mÃ©todos
- MÃ©todos retornam `Result<T>` para sucesso/falha
- Identidade prÃ³pria (invariantes)
- ValidaÃ§Ãµes integradas

| Aspecto                       | Produto (AnÃªmico)              | Pedido (Rico)                            |
| ----------------------------- | ------------------------------- | ---------------------------------------- |
| **Define-se em**              | Apenas propriedades             | Propriedades + mÃ©todos                  |
| **ValidaÃ§Ã£o "PreÃ§o > 0"**  | Em `ProdutoValidator`           | Em `Pedido.Create()`                     |
| **"NÃ£o vender sem estoque"** | Em `ProdutoService`             | Em `Pedido.AddItem()`                    |
| **Quem orquestra?**           | `ProdutoService`                | `Pedido.Create()`, `Pedido.AddItem()`    |
| **Total de Pedido**           | Calculado em `Service`          | Propriedade `Total` do prÃ³prio agregado |
| **Teste**                     | Testa `Service.CancelarAsync()` | Testa `Pedido.Cancel()` direto           |
| **Classe tem identidade?**    | NÃ£o, Ã© apenas storage         | Sim, entidade com regras                 |

---

## 5. Result Pattern

Para distinguir entre sucesso e erro **sem lanÃ§ar exceÃ§Ãµes**, Vertical Slice usa o **Result pattern**:

```csharp
public abstract record Result(bool IsSuccess, string? Error)
{
    public static Result Ok() => new SuccessResult();
    public static Result Fail(string error) => new FailureResult(error);

    public sealed record SuccessResult : Result(true, null);
    public sealed record FailureResult(string ErrorMessage) : Result(false, ErrorMessage);
}

public abstract record Result<T>(bool IsSuccess, T? Value, string? Error)
{
    public static Result<T> Ok(T value) => new SuccessResult(value);
    public static Result<T> Fail(string error) => new FailureResult(error);

    public sealed record SuccessResult(T Value) : Result<T>(true, Value, null);
    public sealed record FailureResult(string ErrorMessage) : Result<T>(false, default, ErrorMessage);
}
```

**Vantagens:**

- Sem overhead de exception handling
- Erros de negÃ³cio sÃ£o esperados
- Code flow Ã© linear e legÃ­vel
- Performance melhor

---

## 6. Quando Usar Cada PadrÃ£o

### Use Clean Architecture (Camadas) quando:

- DomÃ­nio Ã© simples (poucos agregados, poucas regras)
- Muitos endpoints genÃ©ricos (CRUD tradicional)
- Equipe pequena / projeto pequeno
- MudanÃ§as sÃ£o raras e isoladas

**Exemplo:** CatÃ¡logo â€” `Atributo` e `MÃ­dia` (CRUD simples, sem invariantes de negÃ³cio)

### Use Vertical Slice (Feature Folders) quando:

- DomÃ­nio Ã© complexo (muitos agregados, invariantes)
- Cada feature tem lÃ³gica especÃ­fica
- Equipe mÃ©dia/grande
- Escalabilidade horizontal (features independentes)

**Exemplo:** Pedidos â€” lÃ³gica de negÃ³cio embarcada no agregado

---

## 7. Testes em Ambas as Arquiteturas

### Testando Clean Architecture (CatÃ¡logo)

```csharp
[Fact]
public async Task DeletarProduto_DeveRetornarTrue()
{
    // Arrange
    var service = new ProdutoService(context);
    var produto = new Produto { Nome = "Test", Preco = 10 };
    context.Produtos.Add(produto);
    await context.SaveChangesAsync();

    // Act
    var result = await service.DeletarProdutoAsync(produto.Id);

    // Assert
    result.Should().BeTrue();
}
```

**Foco:** Testa comportamento de um serviÃ§o isolado.

### Testando Vertical Slice (Pedido)

```csharp
[Fact]
public void Pedido_AddItem_QuandoStatusNaoAberto_DeveRetornarFalha()
{
    // Arrange
    var pedido = new Pedido { ClienteNome = "Cliente", Status = PedidoStatus.Cancelado };
    var produto = new Produto { Nome = "Test", Preco = 10, Estoque = 100 };

    // Act
    var result = pedido.AddItem(produto, 1);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Error.Should().Be("Pedido nÃ£o estÃ¡ aberto");
}
```

**Foco:** Testa invariantes do agregado direto.

---

## 8. Checklist: Montando um Novo Slice

Quando for adicionar um novo slice de Pedidos:

- [ ] Criar pasta `src/Pedidos/Pedidos.API/NovoSlice/`
- [ ] Criar `NovoSliceCommand.cs` (DTO + Handler)
- [ ] Criar `NovoSliceValidator.cs` (FluentValidation)
- [ ] Criar `NovoSliceEndpoint.cs` (implementa `IEndpoint`)
- [ ] Adicionar método ao agregado `Pedido` (se necessário)
- [ ] Criar testes em `src/Pedidos/Pedidos.Tests/`
- [ ] Testar via `dotnet run` + Swagger

---

## 9. ReferÃªncias no CÃ³digo

### Catálogo (CA Híbrida)

- Endpoints: [src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs](../src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs)
- Service: [src/Catalogo/Catalogo.Application/Services/ProdutoService.cs](../src/Catalogo/Catalogo.Application/Services/ProdutoService.cs)
- Testes: [src/Catalogo/Catalogo.Tests/](../src/Catalogo/Catalogo.Tests/)

### Vertical Slice (Pedidos)

- Domain: [src/Pedidos/Pedidos.Domain/](../src/Pedidos/Pedidos.Domain/)
- CreatePedido: [src/Pedidos/Pedidos.API/CreatePedido/](../src/Pedidos/Pedidos.API/CreatePedido/)
- Result Pattern: [src/Shared/Kernel/Result.cs](../src/Shared/Kernel/Result.cs)
- Testes: [src/Pedidos/Pedidos.Tests/](../src/Pedidos/Pedidos.Tests/)

---

## 10. Comparativo Final

| DimensÃ£o          | CatÃ¡logo (Produto)       | Vertical Slice (Pedidos) |
| ------------------ | ------------------------- | ------------------------ |
| **OrganizaÃ§Ã£o**  | Por camada                | Por feature              |
| **DiretÃ³rio**     | `src/Catalogo/Catalogo.*` | `src/Pedidos/`           |
| **IndependÃªncia** | Fraca (mudanÃ§as globais) | Forte (slice isolada)    |
| **Modelo**         | AnÃªmico / hÃ­brido       | Rico                     |
| **ValidaÃ§Ã£o**    | Em Validator + Service    | No agregado + Validator  |
| **Erro**           | Exception                 | Result pattern           |
| **CoesÃ£o**        | Baixa (espalhada)         | Alta (tudo junto)        |
| **Teste**          | Testa serviÃ§o isolado    | Testa agregado direto    |
| **Escalabilidade** | AtÃ© ~50 endpoints        | 100+ features            |
| **Quando usar**    | DomÃ­nio simples          | DomÃ­nio complexo        |
