# Vertical Slice + DomÃ­nio Rico (Pedidos) Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Adicionar Vertical Slice Architecture para o caso de uso de Pedidos e enriquecer o modelo de domÃ­nio de Produtos, mantendo coexistÃªncia com a arquitetura horizontal em camadas existente.

**Architecture:** O projeto mantÃ©m dois padrÃµes lado a lado: Produtos usa horizontal layers (Endpoints â†’ Services â†’ Data), enquanto Pedidos usa Vertical Slice com Feature Folders (cada operaÃ§Ã£o CRUD Ã© um slice com Command/Query + Handler + Validator + Endpoint). Ambos compartilham o mesmo `AppDbContext` e pipeline de middleware.

**Tech Stack:** .NET 10, Minimal API, Entity Framework Core 10 (SQLite), FluentValidation 11, xUnit, FluentAssertions, EF InMemory (testes).

---

## Task 1: Result Pattern (fundaÃ§Ã£o)

**Files:**
- Create: `src/Features/Common/Result.cs`
- Create: `FacShopAPI.Tests/Unit/Common/ResultTests.cs`

**Step 1: Criar pasta de testes**

```bash
mkdir -p FacShopAPI.Tests/Unit/Common
```

**Step 2: Escrever o teste que falha**

Criar `FacShopAPI.Tests/Unit/Common/ResultTests.cs`:

```csharp
using FluentAssertions;
using FacShopAPI.Features.Common;

namespace FacShopAPI.Tests.Unit.Common;

public class ResultTests
{
    [Fact]
    public void Result_Ok_IsSuccess_True()
    {
        var result = Result.Ok();
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Result_Fail_IsSuccess_False()
    {
        var result = Result.Fail("erro");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("erro");
    }

    [Fact]
    public void ResultT_Ok_CarregaValor()
    {
        var result = Result<int>.Ok(42);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ResultT_Fail_NaoCarregaValor()
    {
        var result = Result<int>.Fail("erro");
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(default);
    }
}
```

**Step 3: Rodar o teste para confirmar que falha**

```bash
dotnet test FacShopAPI.Tests --filter "ResultTests" -v minimal
```

Esperado: FAIL com `namespace 'FacShopAPI.Features.Common' not found`

**Step 4: Criar a pasta e o arquivo**

```bash
mkdir -p src/Features/Common
```

Criar `src/Features/Common/Result.cs`:

```csharp
namespace FacShopAPI.Features.Common;

public record Result(bool IsSuccess, string? Error = null)
{
    public static Result Ok() => new(true);
    public static Result Fail(string error) => new(false, error);
}

public record Result<T>(bool IsSuccess, T? Value, string? Error = null)
{
    public static Result<T> Ok(T value) => new(true, value);
    public static Result<T> Fail(string error) => new(false, default, error);
}
```

**Step 5: Rodar os testes para confirmar que passam**

```bash
dotnet test FacShopAPI.Tests --filter "ResultTests" -v minimal
```

Esperado: 4 testes PASS

**Step 6: Commit**

```bash
git add src/Features/Common/Result.cs FacShopAPI.Tests/Unit/Common/ResultTests.cs
git commit -m "feat: adicionar Result pattern como fundaÃ§Ã£o do domÃ­nio"
```

---

## Task 2: Refatorar Produto para domÃ­nio rico

**Files:**
- Modify: `src/Models/Produto.cs`
- Modify: `src/Data/AppDbContext.cs` (configuraÃ§Ã£o de private setters)
- Modify: `src/Data/DbSeeder.cs` (usar factory method)
- Modify: `src/Services/ProdutoService.cs` (usar mÃ©todos de domÃ­nio)
- Modify: `src/Common/MappingProfile.cs` (ajustar mapeamento)
- Create: `FacShopAPI.Tests/Unit/Domain/ProdutoTests.cs`

**Step 1: Escrever os testes de domÃ­nio que falham**

Criar `FacShopAPI.Tests/Unit/Domain/ProdutoTests.cs`:

```csharp
using FluentAssertions;
using FacShopAPI.Models;

namespace FacShopAPI.Tests.Unit.Domain;

public class ProdutoTests
{
    [Fact]
    public void Criar_ComDadosValidos_RetornaProduto()
    {
        var result = Produto.Criar("Notebook", "Desc", 1000m, "EletrÃ´nicos", 5, "a@b.com");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Nome.Should().Be("Notebook");
        result.Value.Preco.Should().Be(1000m);
    }

    [Fact]
    public void Criar_NomeCurto_RetornaFalha()
    {
        var result = Produto.Criar("AB", "Desc", 100m, "Livros", 1, "a@b.com");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("3");
    }

    [Fact]
    public void Criar_PrecoZero_RetornaFalha()
    {
        var result = Produto.Criar("Notebook", "Desc", 0m, "Livros", 1, "a@b.com");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Criar_EstoqueNegativo_RetornaFalha()
    {
        var result = Produto.Criar("Notebook", "Desc", 100m, "Livros", -1, "a@b.com");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AtualizarPreco_ValorValido_Atualiza()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        var result = produto.AtualizarPreco(200m);

        result.IsSuccess.Should().BeTrue();
        produto.Preco.Should().Be(200m);
    }

    [Fact]
    public void AtualizarPreco_MesmoPreco_RetornaFalha()
    {
        var produto = ProdutoBuilder.Padrao().ComPreco(100m).Build();
        var result = produto.AtualizarPreco(100m);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AtualizarPreco_PrecoZero_RetornaFalha()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        var result = produto.AtualizarPreco(0m);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ReporEstoque_QuantidadePositiva_Adiciona()
    {
        var produto = ProdutoBuilder.Padrao().ComEstoque(10).Build();
        var result = produto.ReporEstoque(5);

        result.IsSuccess.Should().BeTrue();
        produto.Estoque.Should().Be(15);
    }

    [Fact]
    public void ReporEstoque_QuantidadeNegativa_RetornaFalha()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        var result = produto.ReporEstoque(-1);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ReporEstoque_ExcedeMaximo_RetornaFalha()
    {
        var produto = ProdutoBuilder.Padrao().ComEstoque(99_990).Build();
        var result = produto.ReporEstoque(20);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("99999");
    }

    [Fact]
    public void Desativar_ProdutoAtivo_Desativa()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        var result = produto.Desativar();

        result.IsSuccess.Should().BeTrue();
        produto.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Desativar_ProdutoJaInativo_RetornaFalha()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        produto.Desativar();
        var result = produto.Desativar();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void TemEstoqueDisponivel_AtivoComEstoque_RetornaTrue()
    {
        var produto = ProdutoBuilder.Padrao().ComEstoque(10).Build();
        produto.TemEstoqueDisponivel(5).Should().BeTrue();
    }

    [Fact]
    public void TemEstoqueDisponivel_EstoqueInsuficiente_RetornaFalse()
    {
        var produto = ProdutoBuilder.Padrao().ComEstoque(2).Build();
        produto.TemEstoqueDisponivel(5).Should().BeFalse();
    }

    [Fact]
    public void TemEstoqueDisponivel_ProdutoInativo_RetornaFalse()
    {
        var produto = ProdutoBuilder.Padrao().ComEstoque(100).Build();
        produto.Desativar();
        produto.TemEstoqueDisponivel(1).Should().BeFalse();
    }
}
```

**Step 2: Criar o ProdutoBuilder (helper de testes)**

Criar `FacShopAPI.Tests/Builders/ProdutoBuilder.cs`:

```csharp
using FacShopAPI.Models;

namespace FacShopAPI.Tests.Builders;

public class ProdutoBuilder
{
    private string _nome = "Produto Teste";
    private string _descricao = "DescriÃ§Ã£o de teste";
    private decimal _preco = 100m;
    private string _categoria = "EletrÃ´nicos";
    private int _estoque = 10;
    private string _email = "contato@teste.com";

    public static ProdutoBuilder Padrao() => new();

    public ProdutoBuilder ComPreco(decimal preco) { _preco = preco; return this; }
    public ProdutoBuilder ComEstoque(int estoque) { _estoque = estoque; return this; }
    public ProdutoBuilder ComNome(string nome) { _nome = nome; return this; }

    public Produto Build()
    {
        var result = Produto.Criar(_nome, _descricao, _preco, _categoria, _estoque, _email);
        if (!result.IsSuccess) throw new InvalidOperationException(result.Error);
        return result.Value!;
    }
}
```

Adicionar `using FacShopAPI.Tests.Builders;` no arquivo `ProdutoTests.cs`.

**Step 3: Rodar os testes para confirmar que falham**

```bash
dotnet test FacShopAPI.Tests --filter "ProdutoTests" -v minimal
```

Esperado: FAIL com `Produto.Criar nÃ£o existe`

**Step 4: Refatorar `src/Models/Produto.cs`**

Substituir o conteÃºdo completo por:

```csharp
using FacShopAPI.Features.Common;

namespace FacShopAPI.Models;

public class Produto
{
    public static readonly decimal PrecoMinimo = 0.01m;
    public static readonly int EstoqueMaximo = 99_999;

    // EF Core: private setters + private ctor sem parÃ¢metros
    private Produto() { }

    public int Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string Descricao { get; private set; } = "";
    public decimal Preco { get; private set; }
    public string Categoria { get; private set; } = "";
    public int Estoque { get; private set; }
    public bool Ativo { get; private set; } = true;
    public string ContatoEmail { get; private set; } = "";
    public DateTime DataCriacao { get; private set; }
    public DateTime DataAtualizacao { get; private set; }

    public static Result<Produto> Criar(
        string nome, string descricao, decimal preco,
        string categoria, int estoque, string email)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3)
            return Result<Produto>.Fail("Nome deve ter ao menos 3 caracteres.");
        if (preco < PrecoMinimo)
            return Result<Produto>.Fail("PreÃ§o deve ser maior que zero.");
        if (estoque < 0)
            return Result<Produto>.Fail("Estoque nÃ£o pode ser negativo.");
        if (string.IsNullOrWhiteSpace(email))
            return Result<Produto>.Fail("Email de contato Ã© obrigatÃ³rio.");

        var agora = DateTime.UtcNow;
        return Result<Produto>.Ok(new Produto
        {
            Nome = nome,
            Descricao = descricao,
            Preco = preco,
            Categoria = categoria,
            Estoque = estoque,
            ContatoEmail = email,
            Ativo = true,
            DataCriacao = agora,
            DataAtualizacao = agora
        });
    }

    // Usado internamente pelo EF para reconstruir entidade do banco
    internal static Produto Reconstituir(
        int id, string nome, string descricao, decimal preco,
        string categoria, int estoque, bool ativo, string email,
        DateTime criacao, DateTime atualizacao) => new()
    {
        Id = id, Nome = nome, Descricao = descricao, Preco = preco,
        Categoria = categoria, Estoque = estoque, Ativo = ativo,
        ContatoEmail = email, DataCriacao = criacao, DataAtualizacao = atualizacao
    };

    public Result AtualizarPreco(decimal novoPreco)
    {
        if (novoPreco < PrecoMinimo)
            return Result.Fail("PreÃ§o deve ser maior que zero.");
        if (novoPreco == Preco)
            return Result.Fail("Novo preÃ§o Ã© igual ao preÃ§o atual.");
        Preco = novoPreco;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result AtualizarDados(
        string? nome = null, string? descricao = null,
        string? categoria = null, string? email = null)
    {
        if (nome is not null)
        {
            if (nome.Length < 3) return Result.Fail("Nome deve ter ao menos 3 caracteres.");
            Nome = nome;
        }
        if (descricao is not null) Descricao = descricao;
        if (categoria is not null) Categoria = categoria;
        if (email is not null) ContatoEmail = email;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result ReporEstoque(int quantidade)
    {
        if (quantidade <= 0)
            return Result.Fail("Quantidade de reposiÃ§Ã£o deve ser positiva.");
        if (Estoque + quantidade > EstoqueMaximo)
            return Result.Fail($"Estoque nÃ£o pode exceder {EstoqueMaximo} unidades.");
        Estoque += quantidade;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result Desativar()
    {
        if (!Ativo)
            return Result.Fail("Produto jÃ¡ estÃ¡ inativo.");
        Ativo = false;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public bool TemEstoqueDisponivel(int qtd) => Ativo && Estoque >= qtd;
}
```

**Step 5: Atualizar `src/Data/AppDbContext.cs`**

EF Core precisa de acesso aos private setters. Adicionar `UsePropertyAccessMode` na configuraÃ§Ã£o:

No mÃ©todo `OnModelCreating`, dentro do bloco `modelBuilder.Entity<Produto>(entity =>`, adicionar ao final (antes do `}`):

```csharp
// Permite EF Core ler/escrever em propriedades com private setters
entity.Property(p => p.Nome).UsePropertyAccessMode(PropertyAccessMode.Property);
entity.Property(p => p.Descricao).UsePropertyAccessMode(PropertyAccessMode.Property);
entity.Property(p => p.Preco).UsePropertyAccessMode(PropertyAccessMode.Property);
entity.Property(p => p.Categoria).UsePropertyAccessMode(PropertyAccessMode.Property);
entity.Property(p => p.Estoque).UsePropertyAccessMode(PropertyAccessMode.Property);
entity.Property(p => p.Ativo).UsePropertyAccessMode(PropertyAccessMode.Property);
entity.Property(p => p.ContatoEmail).UsePropertyAccessMode(PropertyAccessMode.Property);
entity.Property(p => p.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
entity.Property(p => p.DataAtualizacao).UsePropertyAccessMode(PropertyAccessMode.Property);
```

**Step 6: Atualizar `src/Data/DbSeeder.cs`**

Substituir todos os `new Produto { ... }` por chamadas `Produto.Criar(...)`.

Exemplo de conversÃ£o para o primeiro produto:
```csharp
// ANTES:
new() { Nome = "Notebook Dell XPS 13", ... }

// DEPOIS:
Produto.Criar("Notebook Dell XPS 13",
    "Notebook de alta performance com processador Intel Core i7, 16GB RAM e 512GB SSD",
    4500.00m, "EletrÃ´nicos", 5, "vendas@dell.com").Value!
```

Aplicar o mesmo padrÃ£o para todos os 8 produtos do seed. Remover propriedades `DataCriacao` e `DataAtualizacao` (definidas internamente pelo factory method).

**Step 7: Atualizar `src/Common/MappingProfile.cs`**

AutoMapper nÃ£o pode usar `Produto.Criar()` para mapear `CriarProdutoRequest â†’ Produto` pois a criaÃ§Ã£o agora tem validaÃ§Ã£o. Remover esse mapeamento â€” a criaÃ§Ã£o serÃ¡ feita no `ProdutoService` diretamente.

```csharp
public MappingProfile()
{
    // Mapeamento de Produto para ProdutoResponse (somente leitura)
    CreateMap<Produto, ProdutoResponse>();

    // REMOVIDO: CreateMap<CriarProdutoRequest, Produto>()
    // RazÃ£o: domÃ­nio rico exige criaÃ§Ã£o via Produto.Criar() com validaÃ§Ã£o
}
```

**Step 8: Atualizar `src/Services/ProdutoService.cs`**

Substituir criaÃ§Ã£o e atualizaÃ§Ã£o por mÃ©todos de domÃ­nio.

Em `CriarProdutoAsync`:
```csharp
// ANTES: var produto = _mapper.Map<Produto>(request);
// DEPOIS:
var resultado = Produto.Criar(
    request.Nome, request.Descricao, request.Preco,
    request.Categoria, request.Estoque, request.ContatoEmail);
if (!resultado.IsSuccess)
    throw new InvalidOperationException(resultado.Error);
var produto = resultado.Value!;
// Remover: _context.Produtos.Add(produto) â€” mantÃ©m igual
```

Em `AtualizarProdutoAsync` (PATCH):
```csharp
// ANTES: _mapper.Map(request, produto);
// DEPOIS:
if (request.Preco.HasValue)
{
    var r = produto.AtualizarPreco(request.Preco.Value);
    if (!r.IsSuccess) throw new InvalidOperationException(r.Error);
}
produto.AtualizarDados(request.Nome, request.Descricao, request.Categoria, request.ContatoEmail);
```

Em `AtualizarCompletoProdutoAsync` (PUT):
```csharp
// ANTES: _mapper.Map(request, produto);
// DEPOIS:
var r = produto.AtualizarPreco(request.Preco);
if (!r.IsSuccess) throw new InvalidOperationException(r.Error);
produto.AtualizarDados(request.Nome, request.Descricao, request.Categoria, request.ContatoEmail);
produto.ReporEstoque(request.Estoque - produto.Estoque); // ajuste de estoque
```

Em `DeletarProdutoAsync`:
```csharp
// ANTES: produto.Ativo = false;
// DEPOIS:
produto.Desativar();
```

**Step 9: Build para garantir que compila**

```bash
dotnet build
```

Esperado: Build succeeded sem erros.

**Step 10: Rodar todos os testes**

```bash
dotnet test FacShopAPI.Tests -v minimal
```

Esperado: Todos os testes de ProdutoTests passam. Alguns testes de ProdutoService podem falhar por mudanÃ§as no contrato â€” ajustar conforme necessÃ¡rio.

**Step 11: Commit**

```bash
git add src/Models/Produto.cs src/Data/AppDbContext.cs src/Data/DbSeeder.cs \
        src/Services/ProdutoService.cs src/Common/MappingProfile.cs \
        FacShopAPI.Tests/Unit/Domain/ProdutoTests.cs \
        FacShopAPI.Tests/Builders/ProdutoBuilder.cs
git commit -m "feat: refatorar Produto para modelo de domÃ­nio rico"
```

---

## Task 3: DomÃ­nio de Pedidos (Aggregate Root)

**Files:**
- Create: `src/Features/Pedidos/Domain/StatusPedido.cs`
- Create: `src/Features/Pedidos/Domain/PedidoItem.cs`
- Create: `src/Features/Pedidos/Domain/Pedido.cs`
- Create: `FacShopAPI.Tests/Unit/Domain/PedidoTests.cs`

**Step 1: Escrever os testes que falham**

Criar `FacShopAPI.Tests/Unit/Domain/PedidoTests.cs`:

```csharp
using FluentAssertions;
using FacShopAPI.Features.Pedidos.Domain;
using FacShopAPI.Tests.Builders;

namespace FacShopAPI.Tests.Unit.Domain;

public class PedidoTests
{
    [Fact]
    public void Criar_RetornaPedidoEmRascunho()
    {
        var pedido = Pedido.Criar();
        pedido.Status.Should().Be(StatusPedido.Rascunho);
        pedido.Itens.Should().BeEmpty();
        pedido.Total.Should().Be(0m);
    }

    [Fact]
    public void AdicionarItem_ProdutoValido_AdicionaItem()
    {
        var pedido = Pedido.Criar();
        var produto = ProdutoBuilder.Padrao().ComPreco(50m).ComEstoque(10).Build();

        var result = pedido.AdicionarItem(produto, 2);

        result.IsSuccess.Should().BeTrue();
        pedido.Itens.Should().HaveCount(1);
        pedido.Total.Should().Be(100m);
    }

    [Fact]
    public void AdicionarItem_MesmoProdutoDuasVezes_MergeQuantidade()
    {
        var pedido = Pedido.Criar();
        var produto = ProdutoBuilder.Padrao().ComEstoque(10).Build();

        pedido.AdicionarItem(produto, 2);
        pedido.AdicionarItem(produto, 3);

        pedido.Itens.Should().HaveCount(1);
        pedido.Itens.First().Quantidade.Should().Be(5);
    }

    [Fact]
    public void AdicionarItem_ProdutoSemEstoque_RetornaFalha()
    {
        var pedido = Pedido.Criar();
        var produto = ProdutoBuilder.Padrao().ComEstoque(0).Build();

        var result = pedido.AdicionarItem(produto, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("estoque", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdicionarItem_QuantidadeZero_RetornaFalha()
    {
        var pedido = Pedido.Criar();
        var produto = ProdutoBuilder.Padrao().ComEstoque(10).Build();

        var result = pedido.AdicionarItem(produto, 0);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AdicionarItem_QuantidadeMaiorQue999_RetornaFalha()
    {
        var pedido = Pedido.Criar();
        var produto = ProdutoBuilder.Padrao().ComEstoque(10000).Build();

        var result = pedido.AdicionarItem(produto, 1000);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("999");
    }

    [Fact]
    public void AdicionarItem_MaisDe20ItensdistintosPedido_RetornaFalha()
    {
        var pedido = Pedido.Criar();
        for (int i = 0; i < 20; i++)
        {
            // Cria produto diferente a cada iteraÃ§Ã£o (via builder com nome diferente)
            var p = Produto.Criar($"Produto {i + 100}", "Desc", 10m, "EletrÃ´nicos", 100, "a@b.com").Value!;
            pedido.AdicionarItem(p, 1);
        }
        var novo = ProdutoBuilder.Padrao().ComNome("Produto Extra XXX").Build();

        var result = pedido.AdicionarItem(novo, 1);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("20");
    }

    [Fact]
    public void AdicionarItem_PedidoConfirmado_RetornaFalha()
    {
        var pedido = Pedido.Criar();
        var produto = ProdutoBuilder.Padrao().ComPreco(50m).ComEstoque(10).Build();
        pedido.AdicionarItem(produto, 1);
        pedido.Confirmar();

        var result = pedido.AdicionarItem(produto, 1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Confirmar_ComItensEValorSuficiente_Confirma()
    {
        var pedido = Pedido.Criar();
        var produto = ProdutoBuilder.Padrao().ComPreco(50m).ComEstoque(10).Build();
        pedido.AdicionarItem(produto, 1);

        var result = pedido.Confirmar();

        result.IsSuccess.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Confirmado);
        pedido.ConfirmadoEm.Should().NotBeNull();
    }

    [Fact]
    public void Confirmar_SemItens_RetornaFalha()
    {
        var pedido = Pedido.Criar();
        var result = pedido.Confirmar();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Confirmar_TotalAbaixoDoMinimo_RetornaFalha()
    {
        var pedido = Pedido.Criar();
        var produto = ProdutoBuilder.Padrao().ComPreco(1m).ComEstoque(10).Build();
        pedido.AdicionarItem(produto, 1);

        var result = pedido.Confirmar();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("10,00");
    }

    [Fact]
    public void Confirmar_PedidoJaConfirmado_RetornaFalha()
    {
        var pedido = Pedido.Criar();
        var produto = ProdutoBuilder.Padrao().ComPreco(50m).ComEstoque(10).Build();
        pedido.AdicionarItem(produto, 1);
        pedido.Confirmar();

        var result = pedido.Confirmar();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Cancelar_ComMotivo_Cancela()
    {
        var pedido = Pedido.Criar();
        var result = pedido.Cancelar("DesistÃªncia do cliente");

        result.IsSuccess.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Cancelado);
        pedido.MotivoCancelamento.Should().Be("DesistÃªncia do cliente");
        pedido.CanceladoEm.Should().NotBeNull();
    }

    [Fact]
    public void Cancelar_SemMotivo_RetornaFalha()
    {
        var pedido = Pedido.Criar();
        var result = pedido.Cancelar("");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Cancelar_PedidoJaCancelado_RetornaFalha()
    {
        var pedido = Pedido.Criar();
        pedido.Cancelar("Motivo inicial");
        var result = pedido.Cancelar("Segundo cancelamento");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AdicionarItem_SnapshotDePrecoFixadoNoPedido()
    {
        var pedido = Pedido.Criar();
        var produto = ProdutoBuilder.Padrao().ComPreco(100m).ComEstoque(10).Build();
        pedido.AdicionarItem(produto, 1);

        // Simula alteraÃ§Ã£o de preÃ§o apÃ³s o pedido ser criado
        produto.AtualizarPreco(200m);

        // Item do pedido mantÃ©m o preÃ§o original
        pedido.Itens.First().PrecoUnitario.Should().Be(100m);
    }
}
```

**Step 2: Rodar os testes para confirmar que falham**

```bash
dotnet test FacShopAPI.Tests --filter "PedidoTests" -v minimal
```

Esperado: FAIL com `namespace 'FacShopAPI.Features.Pedidos.Domain' not found`

**Step 3: Criar pastas**

```bash
mkdir -p src/Features/Pedidos/Domain
```

**Step 4: Criar `src/Features/Pedidos/Domain/StatusPedido.cs`**

```csharp
namespace FacShopAPI.Features.Pedidos.Domain;

public enum StatusPedido
{
    Rascunho,
    Confirmado,
    Cancelado
}
```

**Step 5: Criar `src/Features/Pedidos/Domain/PedidoItem.cs`**

```csharp
using FacShopAPI.Models;

namespace FacShopAPI.Features.Pedidos.Domain;

public class PedidoItem
{
    public int Id { get; private set; }
    public int PedidoId { get; private set; }
    public int ProdutoId { get; private set; }
    public string NomeProduto { get; private set; } = "";   // snapshot
    public decimal PrecoUnitario { get; private set; }      // snapshot do momento do pedido
    public int Quantidade { get; private set; }
    public decimal Subtotal => PrecoUnitario * Quantidade;

    private PedidoItem() { }

    internal static PedidoItem Criar(Produto produto, int quantidade) => new()
    {
        ProdutoId = produto.Id,
        NomeProduto = produto.Nome,
        PrecoUnitario = produto.Preco,
        Quantidade = quantidade
    };

    internal void IncrementarQuantidade(int adicional)
    {
        if (Quantidade + adicional > 999)
            throw new InvalidOperationException("Quantidade mÃ¡xima por item Ã© 999.");
        Quantidade += adicional;
    }
}
```

**Step 6: Criar `src/Features/Pedidos/Domain/Pedido.cs`**

```csharp
using FacShopAPI.Features.Common;
using FacShopAPI.Models;

namespace FacShopAPI.Features.Pedidos.Domain;

public class Pedido
{
    public static readonly int MaxItensPorPedido = 20;
    public static readonly decimal ValorMinimoConfirmacao = 10.00m;

    public int Id { get; private set; }
    public StatusPedido Status { get; private set; } = StatusPedido.Rascunho;
    public decimal Total { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? ConfirmadoEm { get; private set; }
    public DateTime? CanceladoEm { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    private readonly List<PedidoItem> _itens = [];
    public IReadOnlyCollection<PedidoItem> Itens => _itens.AsReadOnly();

    private Pedido() { }

    public static Pedido Criar() => new() { CriadoEm = DateTime.UtcNow };

    public Result AdicionarItem(Produto produto, int quantidade)
    {
        if (Status != StatusPedido.Rascunho)
            return Result.Fail("Itens sÃ³ podem ser adicionados a pedidos em rascunho.");

        if (quantidade < 1 || quantidade > 999)
            return Result.Fail("Quantidade deve estar entre 1 e 999.");

        if (!produto.TemEstoqueDisponivel(quantidade))
            return Result.Fail($"Produto '{produto.Nome}' sem estoque suficiente.");

        var existente = _itens.FirstOrDefault(i => i.ProdutoId == produto.Id);
        if (existente is not null)
        {
            existente.IncrementarQuantidade(quantidade);
        }
        else
        {
            if (_itens.Count >= MaxItensPorPedido)
                return Result.Fail($"Pedido nÃ£o pode ter mais de {MaxItensPorPedido} itens distintos.");

            _itens.Add(PedidoItem.Criar(produto, quantidade));
        }

        RecalcularTotal();
        return Result.Ok();
    }

    public Result Confirmar()
    {
        if (Status != StatusPedido.Rascunho)
            return Result.Fail("Apenas pedidos em rascunho podem ser confirmados.");

        if (!_itens.Any())
            return Result.Fail("Pedido precisa ter ao menos um item.");

        if (Total < ValorMinimoConfirmacao)
            return Result.Fail($"Valor mÃ­nimo para confirmaÃ§Ã£o Ã© R$ {ValorMinimoConfirmacao:F2}.");

        Status = StatusPedido.Confirmado;
        ConfirmadoEm = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result Cancelar(string motivo)
    {
        if (Status == StatusPedido.Cancelado)
            return Result.Fail("Pedido jÃ¡ estÃ¡ cancelado.");

        if (string.IsNullOrWhiteSpace(motivo))
            return Result.Fail("Motivo do cancelamento Ã© obrigatÃ³rio.");

        Status = StatusPedido.Cancelado;
        CanceladoEm = DateTime.UtcNow;
        MotivoCancelamento = motivo;
        return Result.Ok();
    }

    private void RecalcularTotal() => Total = _itens.Sum(i => i.Subtotal);
}
```

**Step 7: Rodar os testes de domÃ­nio**

```bash
dotnet test FacShopAPI.Tests --filter "PedidoTests" -v minimal
```

Esperado: Todos os PedidoTests passam.

**Step 8: Commit**

```bash
git add src/Features/Pedidos/Domain/ \
        FacShopAPI.Tests/Unit/Domain/PedidoTests.cs
git commit -m "feat: implementar aggregate Pedido com domÃ­nio rico"
```

---

## Task 4: IEndpoint + Assembly Scanner

**Files:**
- Create: `src/Features/Common/IEndpoint.cs`
- Create: `src/Features/Common/EndpointExtensions.cs`
- Modify: `Program.cs`

**Step 1: Criar `src/Features/Common/IEndpoint.cs`**

```csharp
namespace FacShopAPI.Features.Common;

public interface IEndpoint
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
```

**Step 2: Criar `src/Features/Common/EndpointExtensions.cs`**

```csharp
using System.Reflection;
using FacShopAPI.Features.Common;

namespace FacShopAPI.Features.Common;

public static class EndpointExtensions
{
    /// <summary>
    /// Registra no DI todas as implementaÃ§Ãµes de IEndpoint do assembly.
    /// Elimina a necessidade de registrar cada endpoint manualmente no Program.cs.
    /// </summary>
    public static IServiceCollection AddEndpointsFromAssembly(
        this IServiceCollection services, Assembly assembly)
    {
        var endpointTypes = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.IsAssignableTo(typeof(IEndpoint)));

        foreach (var type in endpointTypes)
            services.AddTransient(typeof(IEndpoint), type);

        return services;
    }

    /// <summary>
    /// Mapeia todos os endpoints registrados via IEndpoint.
    /// </summary>
    public static WebApplication MapRegisteredEndpoints(this WebApplication app)
    {
        var endpoints = app.Services.GetServices<IEndpoint>();
        foreach (var endpoint in endpoints)
            endpoint.MapEndpoints(app);

        return app;
    }
}
```

**Step 3: Atualizar `Program.cs`**

Na seÃ§Ã£o "CONFIGURAÃ‡ÃƒO DE DEPENDENCY INJECTION", adicionar apÃ³s os outros `AddScoped`:

```csharp
// Registrar slices de Pedidos via scan automÃ¡tico
builder.Services.AddEndpointsFromAssembly(typeof(Program).Assembly);
```

Na seÃ§Ã£o "CONFIGURAR ENDPOINTS", adicionar apÃ³s `app.MapProdutoEndpoints()`:

```csharp
// Slices de Pedidos (IEndpoint)
app.MapRegisteredEndpoints();
```

**Step 4: Build para verificar**

```bash
dotnet build
```

Esperado: Build succeeded.

**Step 5: Commit**

```bash
git add src/Features/Common/IEndpoint.cs src/Features/Common/EndpointExtensions.cs Program.cs
git commit -m "feat: adicionar IEndpoint interface e assembly scanner"
```

---

## Task 5: AppDbContext + MigraÃ§Ã£o de Pedidos

**Files:**
- Modify: `src/Data/AppDbContext.cs`

**Step 1: Adicionar DbSets e configuraÃ§Ã£o de Pedido no `AppDbContext.cs`**

Adicionar using no topo do arquivo:
```csharp
using FacShopAPI.Features.Pedidos.Domain;
```

Adicionar DbSets apÃ³s o de Produtos:
```csharp
public DbSet<Pedido> Pedidos => Set<Pedido>();
public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();
```

Adicionar no mÃ©todo `OnModelCreating`, apÃ³s a configuraÃ§Ã£o de `Produto`:

```csharp
modelBuilder.Entity<Pedido>(entity =>
{
    entity.HasKey(p => p.Id);

    entity.Property(p => p.Status)
        .HasConversion<string>()
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(p => p.Total)
        .HasPrecision(10, 2)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(p => p.CriadoEm)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(p => p.ConfirmadoEm)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(p => p.CanceladoEm)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(p => p.MotivoCancelamento)
        .HasMaxLength(500)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    // Mapear a coleÃ§Ã£o privada _itens como backing field
    entity.HasMany<PedidoItem>("_itens")
        .WithOne()
        .HasForeignKey(i => i.PedidoId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.Navigation("_itens").UsePropertyAccessMode(PropertyAccessMode.Field);
});

modelBuilder.Entity<PedidoItem>(entity =>
{
    entity.HasKey(i => i.Id);

    entity.Property(i => i.NomeProduto)
        .IsRequired()
        .HasMaxLength(100)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(i => i.PrecoUnitario)
        .HasPrecision(10, 2)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(i => i.Quantidade)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(i => i.ProdutoId)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    // Subtotal Ã© calculado, nÃ£o persistido
    entity.Ignore(i => i.Subtotal);
});
```

**Step 2: Criar a migration**

```bash
dotnet ef migrations add AddPedidos
```

Esperado: arquivo de migration criado em `Migrations/`.

**Step 3: Verificar o arquivo de migration gerado**

Abrir o arquivo `Migrations/*_AddPedidos.cs` e confirmar que contÃ©m tabelas `Pedidos` e `PedidoItens`.

**Step 4: Build + testes**

```bash
dotnet build && dotnet test FacShopAPI.Tests -v minimal
```

Esperado: Build e todos os testes passam.

**Step 5: Commit**

```bash
git add src/Data/AppDbContext.cs Migrations/
git commit -m "feat: adicionar Pedidos e PedidoItens ao AppDbContext com migration"
```

---

## Task 6: PedidoResponse (DTO compartilhado dos slices)

**Files:**
- Create: `src/Features/Pedidos/Common/PedidoResponse.cs`

**Step 1: Criar `src/Features/Pedidos/Common/PedidoResponse.cs`**

```bash
mkdir -p src/Features/Pedidos/Common
```

```csharp
using FacShopAPI.Features.Pedidos.Domain;

namespace FacShopAPI.Features.Pedidos.Common;

public record PedidoResponse(
    int Id,
    string Status,
    decimal Total,
    DateTime CriadoEm,
    DateTime? ConfirmadoEm,
    DateTime? CanceladoEm,
    string? MotivoCancelamento,
    List<PedidoItemResponse> Itens)
{
    public static PedidoResponse From(Pedido pedido) => new(
        pedido.Id,
        pedido.Status.ToString(),
        pedido.Total,
        pedido.CriadoEm,
        pedido.ConfirmadoEm,
        pedido.CanceladoEm,
        pedido.MotivoCancelamento,
        pedido.Itens.Select(PedidoItemResponse.From).ToList()
    );
}

public record PedidoItemResponse(
    int ProdutoId,
    string NomeProduto,
    decimal PrecoUnitario,
    int Quantidade,
    decimal Subtotal)
{
    public static PedidoItemResponse From(PedidoItem item) => new(
        item.ProdutoId,
        item.NomeProduto,
        item.PrecoUnitario,
        item.Quantidade,
        item.Subtotal
    );
}
```

**Step 2: Build**

```bash
dotnet build
```

**Step 3: Commit**

```bash
git add src/Features/Pedidos/Common/PedidoResponse.cs
git commit -m "feat: adicionar PedidoResponse DTO compartilhado"
```

---

## Task 7: Slice â€” CreatePedido

**Files:**
- Create: `src/Features/Pedidos/CreatePedido/CreatePedidoCommand.cs`
- Create: `src/Features/Pedidos/CreatePedido/CreatePedidoValidator.cs`
- Create: `src/Features/Pedidos/CreatePedido/CreatePedidoEndpoint.cs`

**Step 1: Criar pastas**

```bash
mkdir -p src/Features/Pedidos/CreatePedido
```

**Step 2: Criar `CreatePedidoCommand.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Data;
using FacShopAPI.Features.Common;
using FacShopAPI.Features.Pedidos.Common;
using FacShopAPI.Features.Pedidos.Domain;

namespace FacShopAPI.Features.Pedidos.CreatePedido;

public record CreatePedidoCommand(List<CreatePedidoItemDto> Itens);
public record CreatePedidoItemDto(int ProdutoId, int Quantidade);

public class CreatePedidoHandler(AppDbContext db)
{
    public async Task<Result<PedidoResponse>> HandleAsync(
        CreatePedidoCommand cmd, CancellationToken ct = default)
    {
        var pedido = Pedido.Criar();

        foreach (var itemDto in cmd.Itens)
        {
            var produto = await db.Produtos.FindAsync([itemDto.ProdutoId], ct);
            if (produto is null)
                return Result<PedidoResponse>.Fail($"Produto {itemDto.ProdutoId} nÃ£o encontrado.");

            var resultado = pedido.AdicionarItem(produto, itemDto.Quantidade);
            if (!resultado.IsSuccess)
                return Result<PedidoResponse>.Fail(resultado.Error!);
        }

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);

        return Result<PedidoResponse>.Ok(PedidoResponse.From(pedido));
    }
}
```

**Step 3: Criar `CreatePedidoValidator.cs`**

```csharp
using FluentValidation;

namespace FacShopAPI.Features.Pedidos.CreatePedido;

public class CreatePedidoValidator : AbstractValidator<CreatePedidoCommand>
{
    public CreatePedidoValidator()
    {
        RuleFor(x => x.Itens)
            .NotEmpty()
            .WithMessage("Pedido deve conter ao menos um item.");

        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ProdutoId)
                .GreaterThan(0)
                .WithMessage("ProdutoId invÃ¡lido.");

            item.RuleFor(i => i.Quantidade)
                .InclusiveBetween(1, 999)
                .WithMessage("Quantidade deve estar entre 1 e 999.");
        });
    }
}
```

**Step 4: Criar `CreatePedidoEndpoint.cs`**

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using FacShopAPI.Features.Common;
using FacShopAPI.Features.Pedidos.Common;

namespace FacShopAPI.Features.Pedidos.CreatePedido;

public class CreatePedidoEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
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
            return result.IsSuccess
                ? Results.Created($"/api/v1/pedidos/{result.Value!.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CriarPedido")
        .WithTags("Pedidos")
        .WithSummary("Criar pedido")
        .WithDescription("Cria um novo pedido em status Rascunho com os itens fornecidos")
        .Accepts<CreatePedidoCommand>("application/json")
        .Produces<PedidoResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
    }
}
```

**Step 5: Registrar Handler no DI em `Program.cs`**

Na seÃ§Ã£o "CONFIGURAÃ‡ÃƒO DE DEPENDENCY INJECTION", adicionar:

```csharp
// Handlers dos slices de Pedidos
builder.Services.AddScoped<CreatePedidoHandler>();
```

**Step 6: Build**

```bash
dotnet build
```

**Step 7: Rodar a aplicaÃ§Ã£o e testar via Swagger**

```bash
dotnet run
```

Acessar `http://localhost:5000`, autenticar, e testar `POST /api/v1/pedidos` com:
```json
{
  "itens": [
    { "produtoId": 1, "quantidade": 2 }
  ]
}
```

Esperado: `201 Created` com pedido no corpo.

**Step 8: Commit**

```bash
git add src/Features/Pedidos/CreatePedido/ Program.cs
git commit -m "feat: adicionar slice CreatePedido"
```

---

## Task 8: Slice â€” GetPedido

**Files:**
- Create: `src/Features/Pedidos/GetPedido/GetPedidoQuery.cs`
- Create: `src/Features/Pedidos/GetPedido/GetPedidoEndpoint.cs`

**Step 1: Criar pasta e `GetPedidoQuery.cs`**

```bash
mkdir -p src/Features/Pedidos/GetPedido
```

```csharp
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Data;
using FacShopAPI.Features.Common;
using FacShopAPI.Features.Pedidos.Common;

namespace FacShopAPI.Features.Pedidos.GetPedido;

public record GetPedidoQuery(int Id);

public class GetPedidoHandler(AppDbContext db)
{
    public async Task<Result<PedidoResponse>> HandleAsync(
        GetPedidoQuery query, CancellationToken ct = default)
    {
        var pedido = await db.Pedidos
            .Include("_itens")
            .FirstOrDefaultAsync(p => p.Id == query.Id, ct);

        if (pedido is null)
            return Result<PedidoResponse>.Fail("Pedido nÃ£o encontrado.");

        return Result<PedidoResponse>.Ok(PedidoResponse.From(pedido));
    }
}
```

**Step 2: Criar `GetPedidoEndpoint.cs`**

```csharp
using FacShopAPI.Features.Common;
using FacShopAPI.Features.Pedidos.Common;

namespace FacShopAPI.Features.Pedidos.GetPedido;

public class GetPedidoEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/pedidos/{id:int}", async (
            int id,
            GetPedidoHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetPedidoQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("ObterPedido")
        .WithTags("Pedidos")
        .WithSummary("Obter pedido por ID")
        .Produces<PedidoResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
```

**Step 3: Registrar Handler no DI em `Program.cs`**

```csharp
builder.Services.AddScoped<GetPedidoHandler>();
```

**Step 4: Build**

```bash
dotnet build
```

**Step 5: Commit**

```bash
git add src/Features/Pedidos/GetPedido/ Program.cs
git commit -m "feat: adicionar slice GetPedido"
```

---

## Task 9: Slice â€” ListPedidos

**Files:**
- Create: `src/Features/Pedidos/ListPedidos/ListPedidosQuery.cs`
- Create: `src/Features/Pedidos/ListPedidos/ListPedidosEndpoint.cs`

**Step 1: Criar pasta e `ListPedidosQuery.cs`**

```bash
mkdir -p src/Features/Pedidos/ListPedidos
```

```csharp
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Data;
using FacShopAPI.Features.Common;
using FacShopAPI.Features.Pedidos.Common;
using FacShopAPI.Features.Pedidos.Domain;

namespace FacShopAPI.Features.Pedidos.ListPedidos;

public record ListPedidosQuery(int Page = 1, int PageSize = 20, string? Status = null);

public record ListPedidosResponse(List<PedidoResponse> Data, int Total, int Page, int PageSize);

public class ListPedidosHandler(AppDbContext db)
{
    public async Task<Result<ListPedidosResponse>> HandleAsync(
        ListPedidosQuery query, CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);

        var q = db.Pedidos.Include("_itens").AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<StatusPedido>(query.Status, true, out var status))
        {
            q = q.Where(p => p.Status == status);
        }

        var total = await q.CountAsync(ct);
        var pedidos = await q
            .OrderByDescending(p => p.CriadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var response = new ListPedidosResponse(
            pedidos.Select(PedidoResponse.From).ToList(),
            total, page, pageSize);

        return Result<ListPedidosResponse>.Ok(response);
    }
}
```

**Step 2: Criar `ListPedidosEndpoint.cs`**

```csharp
using FacShopAPI.Features.Common;
using FacShopAPI.Features.Pedidos.Common;

namespace FacShopAPI.Features.Pedidos.ListPedidos;

public class ListPedidosEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/pedidos", async (
            ListPedidosHandler handler,
            CancellationToken ct,
            int page = 1,
            int pageSize = 20,
            string? status = null) =>
        {
            var result = await handler.HandleAsync(
                new ListPedidosQuery(page, pageSize, status), ct);
            return Results.Ok(result.Value);
        })
        .WithName("ListarPedidos")
        .WithTags("Pedidos")
        .WithSummary("Listar pedidos com paginaÃ§Ã£o")
        .Produces<ListPedidosResponse>(StatusCodes.Status200OK)
        .RequireAuthorization();
    }
}
```

**Step 3: Registrar Handler no DI em `Program.cs`**

```csharp
builder.Services.AddScoped<ListPedidosHandler>();
```

**Step 4: Build**

```bash
dotnet build
```

**Step 5: Commit**

```bash
git add src/Features/Pedidos/ListPedidos/ Program.cs
git commit -m "feat: adicionar slice ListPedidos com paginaÃ§Ã£o e filtro por status"
```

---

## Task 10: Slice â€” AddItemPedido

**Files:**
- Create: `src/Features/Pedidos/AddItemPedido/AddItemCommand.cs`
- Create: `src/Features/Pedidos/AddItemPedido/AddItemValidator.cs`
- Create: `src/Features/Pedidos/AddItemPedido/AddItemEndpoint.cs`

**Step 1: Criar pasta e `AddItemCommand.cs`**

```bash
mkdir -p src/Features/Pedidos/AddItemPedido
```

```csharp
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Data;
using FacShopAPI.Features.Common;
using FacShopAPI.Features.Pedidos.Common;

namespace FacShopAPI.Features.Pedidos.AddItemPedido;

public record AddItemCommand(int PedidoId, int ProdutoId, int Quantidade);

public class AddItemHandler(AppDbContext db)
{
    public async Task<Result<PedidoResponse>> HandleAsync(
        AddItemCommand cmd, CancellationToken ct = default)
    {
        var pedido = await db.Pedidos
            .Include("_itens")
            .FirstOrDefaultAsync(p => p.Id == cmd.PedidoId, ct);

        if (pedido is null)
            return Result<PedidoResponse>.Fail("Pedido nÃ£o encontrado.");

        var produto = await db.Produtos.FindAsync([cmd.ProdutoId], ct);
        if (produto is null)
            return Result<PedidoResponse>.Fail($"Produto {cmd.ProdutoId} nÃ£o encontrado.");

        var resultado = pedido.AdicionarItem(produto, cmd.Quantidade);
        if (!resultado.IsSuccess)
            return Result<PedidoResponse>.Fail(resultado.Error!);

        await db.SaveChangesAsync(ct);
        return Result<PedidoResponse>.Ok(PedidoResponse.From(pedido));
    }
}
```

**Step 2: Criar `AddItemValidator.cs`**

```csharp
using FluentValidation;

namespace FacShopAPI.Features.Pedidos.AddItemPedido;

public record AddItemRequest(int ProdutoId, int Quantidade);

public class AddItemValidator : AbstractValidator<AddItemRequest>
{
    public AddItemValidator()
    {
        RuleFor(x => x.ProdutoId).GreaterThan(0).WithMessage("ProdutoId invÃ¡lido.");
        RuleFor(x => x.Quantidade)
            .InclusiveBetween(1, 999)
            .WithMessage("Quantidade deve estar entre 1 e 999.");
    }
}
```

**Step 3: Criar `AddItemEndpoint.cs`**

```csharp
using FluentValidation;
using FacShopAPI.Features.Common;
using FacShopAPI.Features.Pedidos.Common;

namespace FacShopAPI.Features.Pedidos.AddItemPedido;

public class AddItemEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/pedidos/{id:int}/itens", async (
            int id,
            AddItemRequest request,
            AddItemHandler handler,
            IValidator<AddItemRequest> validator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var cmd = new AddItemCommand(id, request.ProdutoId, request.Quantidade);
            var result = await handler.HandleAsync(cmd, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("AdicionarItemPedido")
        .WithTags("Pedidos")
        .WithSummary("Adicionar item a pedido em rascunho")
        .Produces<PedidoResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
    }
}
```

**Step 4: Registrar no DI e no scanner de validators em `Program.cs`**

```csharp
builder.Services.AddScoped<AddItemHandler>();
```

Os validators dos slices sÃ£o descobertos automaticamente pelo `AddValidatorsFromAssemblyContaining<CriarProdutoValidator>()` que jÃ¡ escaneia todo o assembly.

**Step 5: Build**

```bash
dotnet build
```

**Step 6: Commit**

```bash
git add src/Features/Pedidos/AddItemPedido/ Program.cs
git commit -m "feat: adicionar slice AddItemPedido"
```

---

## Task 11: Slice â€” CancelPedido

**Files:**
- Create: `src/Features/Pedidos/CancelPedido/CancelPedidoCommand.cs`
- Create: `src/Features/Pedidos/CancelPedido/CancelPedidoEndpoint.cs`

**Step 1: Criar pasta e `CancelPedidoCommand.cs`**

```bash
mkdir -p src/Features/Pedidos/CancelPedido
```

```csharp
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Data;
using FacShopAPI.Features.Common;
using FacShopAPI.Features.Pedidos.Common;

namespace FacShopAPI.Features.Pedidos.CancelPedido;

public record CancelPedidoCommand(int PedidoId, string Motivo);

public class CancelPedidoHandler(AppDbContext db)
{
    public async Task<Result<PedidoResponse>> HandleAsync(
        CancelPedidoCommand cmd, CancellationToken ct = default)
    {
        var pedido = await db.Pedidos
            .Include("_itens")
            .FirstOrDefaultAsync(p => p.Id == cmd.PedidoId, ct);

        if (pedido is null)
            return Result<PedidoResponse>.Fail("Pedido nÃ£o encontrado.");

        var resultado = pedido.Cancelar(cmd.Motivo);
        if (!resultado.IsSuccess)
            return Result<PedidoResponse>.Fail(resultado.Error!);

        await db.SaveChangesAsync(ct);
        return Result<PedidoResponse>.Ok(PedidoResponse.From(pedido));
    }
}
```

**Step 2: Criar `CancelPedidoEndpoint.cs`**

```csharp
using FacShopAPI.Features.Common;
using FacShopAPI.Features.Pedidos.Common;

namespace FacShopAPI.Features.Pedidos.CancelPedido;

public class CancelPedidoEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/pedidos/{id:int}/cancelar", async (
            int id,
            CancelPedidoRequest request,
            CancelPedidoHandler handler,
            CancellationToken ct) =>
        {
            var cmd = new CancelPedidoCommand(id, request.Motivo);
            var result = await handler.HandleAsync(cmd, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CancelarPedido")
        .WithTags("Pedidos")
        .WithSummary("Cancelar pedido")
        .Produces<PedidoResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization();
    }
}

public record CancelPedidoRequest(string Motivo);
```

**Step 3: Registrar Handler no DI em `Program.cs`**

```csharp
builder.Services.AddScoped<CancelPedidoHandler>();
```

**Step 4: Build**

```bash
dotnet build
```

**Step 5: Commit**

```bash
git add src/Features/Pedidos/CancelPedido/ Program.cs
git commit -m "feat: adicionar slice CancelPedido"
```

---

## Task 12: Testes de integraÃ§Ã£o dos slices

**Files:**
- Modify: `FacShopAPI.Tests/FacShopAPI.Tests.csproj` (adicionar Microsoft.AspNetCore.Mvc.Testing)
- Create: `FacShopAPI.Tests/Integration/Pedidos/CreatePedidoTests.cs`
- Create: `FacShopAPI.Tests/Integration/Pedidos/GetPedidoTests.cs`
- Create: `FacShopAPI.Tests/Integration/Pedidos/CancelPedidoTests.cs`

**Step 1: Adicionar dependÃªncia de testes de integraÃ§Ã£o**

Em `FacShopAPI.Tests/FacShopAPI.Tests.csproj`, dentro de `<ItemGroup>`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
```

Restaurar:
```bash
dotnet restore FacShopAPI.Tests
```

**Step 2: Criar factory helper**

Criar `FacShopAPI.Tests/Integration/ApiFactory.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FacShopAPI.Data;

namespace FacShopAPI.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Substituir SQLite por InMemory em testes
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
        });
    }
}
```

**Step 3: Criar helper de autenticaÃ§Ã£o**

Criar `FacShopAPI.Tests/Integration/AuthHelper.cs`:

```csharp
using System.Net.Http.Json;

namespace FacShopAPI.Tests.Integration;

public static class AuthHelper
{
    public static async Task<string> ObterTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "admin@produtos.com",
            Senha = "Admin@123456"
        });
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return result!.Token;
    }

    private record TokenResponse(string Token);
}
```

**Step 4: Criar `CreatePedidoTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using FacShopAPI.Features.Pedidos.Common;
using FacShopAPI.Features.Pedidos.CreatePedido;

namespace FacShopAPI.Tests.Integration.Pedidos;

public class CreatePedidoTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public CreatePedidoTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var token = await AuthHelper.ObterTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task POST_Pedidos_SemAutenticacao_Retorna401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/pedidos",
            new CreatePedidoCommand([new(1, 2)]));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Pedidos_SemItens_Retorna422()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/pedidos",
            new CreatePedidoCommand([]));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_Pedidos_ProdutoInexistente_Retorna400()
    {
        await AuthenticateAsync();
        var response = await _client.PostAsJsonAsync("/api/v1/pedidos",
            new CreatePedidoCommand([new(99999, 1)]));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Pedidos_ComProdutoValido_Retorna201()
    {
        await AuthenticateAsync();
        // Produto com Id=1 Ã© seed pelo DbSeeder
        var response = await _client.PostAsJsonAsync("/api/v1/pedidos",
            new CreatePedidoCommand([new(1, 1)]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var pedido = await response.Content.ReadFromJsonAsync<PedidoResponse>();
        pedido.Should().NotBeNull();
        pedido!.Status.Should().Be("Rascunho");
        pedido.Itens.Should().HaveCount(1);
        response.Headers.Location.Should().NotBeNull();
    }
}
```

**Step 5: Criar `GetPedidoTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using FacShopAPI.Features.Pedidos.Common;
using FacShopAPI.Features.Pedidos.CreatePedido;

namespace FacShopAPI.Tests.Integration.Pedidos;

public class GetPedidoTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public GetPedidoTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var token = await AuthHelper.ObterTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GET_Pedido_NaoExistente_Retorna404()
    {
        await AuthenticateAsync();
        var response = await _client.GetAsync("/api/v1/pedidos/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Pedido_Existente_Retorna200ComDados()
    {
        await AuthenticateAsync();

        // Criar um pedido primeiro
        var createResponse = await _client.PostAsJsonAsync("/api/v1/pedidos",
            new CreatePedidoCommand([new(1, 2)]));
        var criado = await createResponse.Content.ReadFromJsonAsync<PedidoResponse>();

        // Buscar o pedido criado
        var response = await _client.GetAsync($"/api/v1/pedidos/{criado!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pedido = await response.Content.ReadFromJsonAsync<PedidoResponse>();
        pedido!.Id.Should().Be(criado.Id);
        pedido.Itens.Should().HaveCount(1);
    }
}
```

**Step 6: Criar `CancelPedidoTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using FacShopAPI.Features.Pedidos.CancelPedido;
using FacShopAPI.Features.Pedidos.Common;
using FacShopAPI.Features.Pedidos.CreatePedido;

namespace FacShopAPI.Tests.Integration.Pedidos;

public class CancelPedidoTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public CancelPedidoTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var token = await AuthHelper.ObterTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task POST_Cancelar_SemMotivo_Retorna400()
    {
        await AuthenticateAsync();
        var create = await _client.PostAsJsonAsync("/api/v1/pedidos",
            new CreatePedidoCommand([new(1, 1)]));
        var pedido = await create.Content.ReadFromJsonAsync<PedidoResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/pedidos/{pedido!.Id}/cancelar",
            new CancelPedidoRequest(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Cancelar_ComMotivo_Retorna200ECancela()
    {
        await AuthenticateAsync();
        var create = await _client.PostAsJsonAsync("/api/v1/pedidos",
            new CreatePedidoCommand([new(1, 1)]));
        var pedido = await create.Content.ReadFromJsonAsync<PedidoResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/pedidos/{pedido!.Id}/cancelar",
            new CancelPedidoRequest("Teste de cancelamento"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelado = await response.Content.ReadFromJsonAsync<PedidoResponse>();
        cancelado!.Status.Should().Be("Cancelado");
        cancelado.MotivoCancelamento.Should().Be("Teste de cancelamento");
    }
}
```

**Step 7: Rodar todos os testes**

```bash
dotnet test FacShopAPI.Tests -v minimal
```

Esperado: Todos os testes passam (unit + integration).

**Step 8: Commit final**

```bash
git add FacShopAPI.Tests/
git commit -m "test: adicionar testes de integraÃ§Ã£o dos slices de Pedidos"
```

---

## VerificaÃ§Ã£o Final

**Step 1: Build limpo**

```bash
dotnet build -c Release
```

**Step 2: Todos os testes**

```bash
dotnet test FacShopAPI.Tests -v normal
```

Esperado: Todos os testes passam, cobertura de domÃ­nio + integraÃ§Ã£o.

**Step 3: Executar a aplicaÃ§Ã£o**

```bash
dotnet run
```

Acessar `http://localhost:5000` e verificar no Swagger que os endpoints de Pedidos aparecem corretamente na tag "Pedidos", com auth configurado.

**Step 4: Commit de encerramento**

```bash
git add .
git commit -m "chore: verificaÃ§Ã£o final â€” Vertical Slice + domÃ­nio rico completos"
```
