# Fase 1 â€” Migrar Produtos â†’ CatÃ¡logo

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Renomear o bounded context `Produtos` para `CatÃ¡logo`, reorganizar rotas para `/api/v1/catalogo/produtos`, e preparar a estrutura para os novos recursos.

**Architecture:** 4 sub-projetos existentes (`Produtos.Domain`, `.Application`, `.Infrastructure`, `.API`) sÃ£o renomeados para `Catalogo.*`. Namespaces mudam de `FacShopAPI.Produtos.*` para `FacShopAPI.Catalogo.*`. `IProdutoContext` vira `ICatalogoContext`. Route group manual: `app.MapGroup("/api/v1/catalogo")`.

**Tech Stack:** .NET 10, EF Core 10, Dapper, FluentValidation, AutoMapper, xUnit, FluentAssertions.

---

### Task 1: Criar os 4 csproj de Catalogo

**Files:**

- Create: `src/Catalogo/Catalogo.Domain/Catalogo.Domain.csproj`
- Create: `src/Catalogo/Catalogo.Application/Catalogo.Application.csproj`
- Create: `src/Catalogo/Catalogo.Infrastructure/Catalogo.Infrastructure.csproj`
- Create: `src/Catalogo/Catalogo.API/Catalogo.API.csproj`

- [ ] **Step 1: Criar diretÃ³rios**

```bash
mkdir -p src/Catalogo/Catalogo.Domain/Common
mkdir -p src/Catalogo/Catalogo.Domain/ValueObjects
mkdir -p src/Catalogo/Catalogo.Application/DTOs/Produto
mkdir -p src/Catalogo/Catalogo.Application/Interfaces
mkdir -p src/Catalogo/Catalogo.Application/Repositories
mkdir -p src/Catalogo/Catalogo.Application/Services
mkdir -p src/Catalogo/Catalogo.Application/Validators
mkdir -p src/Catalogo/Catalogo.Application/Mappings
mkdir -p src/Catalogo/Catalogo.Infrastructure/Repositories
mkdir -p src/Catalogo/Catalogo.Infrastructure/Queries
mkdir -p src/Catalogo/Catalogo.Infrastructure/Data
mkdir -p src/Catalogo/Catalogo.API/Endpoints/Produtos
mkdir -p src/Catalogo/Catalogo.API/Endpoints/Auth
mkdir -p src/Catalogo/Catalogo.API/Extensions
```

- [ ] **Step 2: Criar Catalogo.Domain.csproj**

Crie `src/Catalogo/Catalogo.Domain/Catalogo.Domain.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>FacShopAPI.Catalogo.Domain</RootNamespace>
    <AssemblyName>Catalogo.Domain</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
      <_Parameter1>FacShopAPI.Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Criar Catalogo.Application.csproj**

Crie `src/Catalogo/Catalogo.Application/Catalogo.Application.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>FacShopAPI.Catalogo.Application</RootNamespace>
    <AssemblyName>Catalogo.Application</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Catalogo.Domain/Catalogo.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="AutoMapper" Version="16.1.1" />
    <PackageReference Include="FluentValidation" Version="11.10.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Criar Catalogo.Infrastructure.csproj**

Crie `src/Catalogo/Catalogo.Infrastructure/Catalogo.Infrastructure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>FacShopAPI.Catalogo.Infrastructure</RootNamespace>
    <AssemblyName>Catalogo.Infrastructure</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Catalogo.Application/Catalogo.Application.csproj" />
    <ProjectReference Include="../Catalogo.Domain/Catalogo.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Dapper" Version="2.1.66" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Criar Catalogo.API.csproj**

Crie `src/Catalogo/Catalogo.API/Catalogo.API.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>FacShopAPI.Catalogo.API</RootNamespace>
    <AssemblyName>Catalogo.API</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../Catalogo.Application/Catalogo.Application.csproj" />
    <ProjectReference Include="../Catalogo.Infrastructure/Catalogo.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="11.10.0" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.10.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.1" />
  </ItemGroup>
</Project>
```

---

### Task 2: Criar Catalogo.Domain â€” Result, ValueObjects e Produto

**Files:**

- Create: `src/Catalogo/Catalogo.Domain/Common/Result.cs`
- Create: `src/Catalogo/Catalogo.Domain/ValueObjects/PrecoProduto.cs`
- Create: `src/Catalogo/Catalogo.Domain/ValueObjects/EstoqueProduto.cs`
- Create: `src/Catalogo/Catalogo.Domain/ValueObjects/DescricaoProduto.cs`
- Create: `src/Catalogo/Catalogo.Domain/ValueObjects/CategoriaProduto.cs`
- Create: `src/Catalogo/Catalogo.Domain/Produto.cs`

- [ ] **Step 1: Criar Result.cs**

Crie `src/Catalogo/Catalogo.Domain/Common/Result.cs`:

```csharp
namespace FacShopAPI.Catalogo.Domain.Common;

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

- [ ] **Step 2: Criar PrecoProduto.cs**

Crie `src/Catalogo/Catalogo.Domain/ValueObjects/PrecoProduto.cs`:

```csharp
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Domain.ValueObjects;

public sealed record PrecoProduto
{
    public const decimal Minimo = 0.01m;
    private PrecoProduto(decimal value) => Value = value;
    public decimal Value { get; }

    public static Result<PrecoProduto> Criar(decimal value)
    {
        if (value < Minimo)
            return Result<PrecoProduto>.Fail("PreÃ§o deve ser maior que zero.");
        return Result<PrecoProduto>.Ok(new PrecoProduto(value));
    }

    public static PrecoProduto Reconstituir(decimal value)
    {
        var result = Criar(value);
        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException(result.Error);
        return result.Value;
    }

    public override string ToString() => Value.ToString("0.00");
    public static implicit operator decimal(PrecoProduto preco) => preco.Value;
}
```

- [ ] **Step 3: Criar EstoqueProduto.cs**

Crie `src/Catalogo/Catalogo.Domain/ValueObjects/EstoqueProduto.cs`:

```csharp
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Domain.ValueObjects;

public sealed record EstoqueProduto
{
    public const int Maximo = 99_999;
    private EstoqueProduto(int value) => Value = value;
    public int Value { get; }

    public static Result<EstoqueProduto> Criar(int value)
    {
        if (value < 0)
            return Result<EstoqueProduto>.Fail("Estoque nÃ£o pode ser negativo.");
        if (value > Maximo)
            return Result<EstoqueProduto>.Fail($"Estoque nÃ£o pode exceder {Maximo} unidades.");
        return Result<EstoqueProduto>.Ok(new EstoqueProduto(value));
    }

    public static EstoqueProduto Reconstituir(int value) => new(value);
    public static implicit operator int(EstoqueProduto estoque) => estoque.Value;
}
```

- [ ] **Step 4: Criar DescricaoProduto.cs**

Crie `src/Catalogo/Catalogo.Domain/ValueObjects/DescricaoProduto.cs`:

```csharp
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Domain.ValueObjects;

public sealed record DescricaoProduto
{
    private DescricaoProduto(string value) => Value = value;
    public string Value { get; }

    public static Result<DescricaoProduto> Criar(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 10)
            return Result<DescricaoProduto>.Fail("DescriÃ§Ã£o deve ter ao menos 10 caracteres.");
        if (value.Length > 500)
            return Result<DescricaoProduto>.Fail("DescriÃ§Ã£o nÃ£o pode exceder 500 caracteres.");
        return Result<DescricaoProduto>.Ok(new DescricaoProduto(value));
    }

    public static DescricaoProduto Reconstituir(string value) => new(value);
    public override string ToString() => Value;
}
```

- [ ] **Step 5: Criar CategoriaProduto.cs**

Crie `src/Catalogo/Catalogo.Domain/ValueObjects/CategoriaProduto.cs`:

```csharp
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Domain.ValueObjects;

public sealed record CategoriaProduto
{
    private static readonly string[] CategoriasValidas =
        ["EletrÃ´nicos", "Livros", "Roupas", "Alimentos", "Outros"];

    private CategoriaProduto(string value) => Value = value;
    public string Value { get; }

    public static Result<CategoriaProduto> Criar(string value)
    {
        if (!CategoriasValidas.Contains(value))
            return Result<CategoriaProduto>.Fail(
                $"Categoria invÃ¡lida. VÃ¡lidas: {string.Join(", ", CategoriasValidas)}");
        return Result<CategoriaProduto>.Ok(new CategoriaProduto(value));
    }

    public static CategoriaProduto Reconstituir(string value) => new(value);
    public override string ToString() => Value;
}
```

- [ ] **Step 6: Criar Produto.cs**

Crie `src/Catalogo/Catalogo.Domain/Produto.cs` â€” cÃ³pia do arquivo original com namespaces atualizados:

```csharp
using FacShopAPI.Catalogo.Domain.Common;
using FacShopAPI.Catalogo.Domain.ValueObjects;

namespace FacShopAPI.Catalogo.Domain;

public class Produto
{
    public static readonly decimal PrecoMinimo = PrecoProduto.Minimo;
    public static readonly int EstoqueMaximo = EstoqueProduto.Maximo;

    private Produto() { }

    public int Id { get; private set; }
    public string Nome { get; private set; } = "";
    public DescricaoProduto Descricao { get; private set; } = null!;
    public PrecoProduto Preco { get; private set; } = null!;
    public CategoriaProduto Categoria { get; private set; } = null!;
    public EstoqueProduto Estoque { get; private set; } = null!;
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
        if (string.IsNullOrWhiteSpace(email))
            return Result<Produto>.Fail("Email de contato Ã© obrigatÃ³rio.");

        var descricaoResult = DescricaoProduto.Criar(descricao);
        if (!descricaoResult.IsSuccess) return Result<Produto>.Fail(descricaoResult.Error!);

        var precoResult = PrecoProduto.Criar(preco);
        if (!precoResult.IsSuccess) return Result<Produto>.Fail(precoResult.Error!);

        var categoriaResult = CategoriaProduto.Criar(categoria);
        if (!categoriaResult.IsSuccess) return Result<Produto>.Fail(categoriaResult.Error!);

        var estoqueResult = EstoqueProduto.Criar(estoque);
        if (!estoqueResult.IsSuccess) return Result<Produto>.Fail(estoqueResult.Error!);

        var agora = DateTime.UtcNow;
        return Result<Produto>.Ok(new Produto
        {
            Nome = nome,
            Descricao = descricaoResult.Value!,
            Preco = precoResult.Value!,
            Categoria = categoriaResult.Value!,
            Estoque = estoqueResult.Value!,
            ContatoEmail = email,
            Ativo = true,
            DataCriacao = agora,
            DataAtualizacao = agora
        });
    }

    public static Produto Reconstituir(
        int id, string nome, string descricao, decimal preco,
        string categoria, int estoque, bool ativo,
        string contatoEmail, DateTime dataCriacao, DateTime dataAtualizacao)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        return new Produto
        {
            Id = id, Nome = nome,
            Descricao = DescricaoProduto.Reconstituir(descricao),
            Preco = PrecoProduto.Reconstituir(preco),
            Categoria = CategoriaProduto.Reconstituir(categoria),
            Estoque = EstoqueProduto.Reconstituir(estoque),
            Ativo = ativo, ContatoEmail = contatoEmail,
            DataCriacao = dataCriacao, DataAtualizacao = dataAtualizacao
        };
    }

    public Result AtualizarPreco(decimal novoPreco)
    {
        var precoResult = PrecoProduto.Criar(novoPreco);
        if (!precoResult.IsSuccess) return Result.Fail(precoResult.Error!);
        if (precoResult.Value!.Value == Preco.Value) return Result.Fail("Novo preÃ§o Ã© igual ao preÃ§o atual.");
        Preco = precoResult.Value;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result AtualizarDados(string? nome = null, string? descricao = null,
        string? categoria = null, string? email = null)
    {
        if (nome is not null)
        {
            if (nome.Length < 3) return Result.Fail("Nome deve ter ao menos 3 caracteres.");
            Nome = nome;
        }
        if (descricao is not null)
        {
            var r = DescricaoProduto.Criar(descricao);
            if (!r.IsSuccess) return Result.Fail(r.Error!);
            Descricao = r.Value!;
        }
        if (categoria is not null)
        {
            var r = CategoriaProduto.Criar(categoria);
            if (!r.IsSuccess) return Result.Fail(r.Error!);
            Categoria = r.Value!;
        }
        if (email is not null)
        {
            if (string.IsNullOrWhiteSpace(email)) return Result.Fail("Email de contato Ã© obrigatÃ³rio.");
            ContatoEmail = email;
        }
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result ReporEstoque(int quantidade)
    {
        if (quantidade <= 0) return Result.Fail("Quantidade de reposiÃ§Ã£o deve ser positiva.");
        var novoEstoque = Estoque.Value + quantidade;
        if (novoEstoque > EstoqueMaximo)
            return Result.Fail($"Estoque nÃ£o pode exceder {EstoqueMaximo} unidades.");
        Estoque = EstoqueProduto.Reconstituir(novoEstoque);
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result Desativar()
    {
        if (!Ativo) return Result.Fail("Produto jÃ¡ estÃ¡ inativo.");
        Ativo = false;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public bool TemEstoqueDisponivel(int qtd) => Ativo && Estoque.Value >= qtd;

    public void AjustarEstoque(int quantidade)
    {
        var r = EstoqueProduto.Criar(quantidade);
        if (!r.IsSuccess) throw new InvalidOperationException(r.Error);
        Estoque = r.Value!;
        DataAtualizacao = DateTime.UtcNow;
    }

    internal void SetIdForTesting(int id) => Id = id;
}
```

- [ ] **Step 7: Build Catalogo.Domain**

```bash
dotnet build src/Catalogo/Catalogo.Domain/Catalogo.Domain.csproj
```

Esperado: `Build succeeded, 0 errors`.

---

### Task 3: Criar Catalogo.Application

**Files:**

- Create: `src/Catalogo/Catalogo.Application/Interfaces/ICatalogoContext.cs`
- Create: `src/Catalogo/Catalogo.Application/Repositories/IProdutoCommandRepository.cs`
- Create: `src/Catalogo/Catalogo.Application/Repositories/IProdutoQueryRepository.cs`
- Create: `src/Catalogo/Catalogo.Application/DTOs/Produto/ProdutoDTO.cs`
- Create: `src/Catalogo/Catalogo.Application/Services/IProdutoService.cs`
- Create: `src/Catalogo/Catalogo.Application/Services/ProdutoService.cs`
- Create: `src/Catalogo/Catalogo.Application/Validators/ProdutoValidator.cs`
- Create: `src/Catalogo/Catalogo.Application/Mappings/ProdutoMappingProfile.cs`

- [ ] **Step 1: Criar ICatalogoContext.cs**

Crie `src/Catalogo/Catalogo.Application/Interfaces/ICatalogoContext.cs`:

```csharp
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Interfaces;

public interface ICatalogoContext
{
    IQueryable<Produto> Produtos { get; }
    void AddProduto(Produto produto);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Criar IProdutoCommandRepository.cs**

Crie `src/Catalogo/Catalogo.Application/Repositories/IProdutoCommandRepository.cs`:

```csharp
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IProdutoCommandRepository
{
    Task<Produto?> ObterPorIdAsync(int id);
    Task<Produto> AdicionarAsync(Produto produto);
    Task<bool> DeletarAsync(int id);
    Task SaveChangesAsync();
}
```

- [ ] **Step 3: Criar IProdutoQueryRepository.cs**

Crie `src/Catalogo/Catalogo.Application/Repositories/IProdutoQueryRepository.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Produto;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IProdutoQueryRepository
{
    Task<ProdutoResponse?> ObterPorIdAsync(int id);
    Task<(IReadOnlyList<ProdutoResponse> Items, int Total)> ListarAsync(
        int page, int pageSize, string? categoria = null, string? search = null);
}
```

- [ ] **Step 4: Criar ProdutoDTO.cs**

Crie `src/Catalogo/Catalogo.Application/DTOs/Produto/ProdutoDTO.cs`:

```csharp
namespace FacShopAPI.Catalogo.Application.DTOs.Produto;

public class CriarProdutoRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public int Estoque { get; set; }
    public string ContatoEmail { get; set; } = string.Empty;
}

public class AtualizarProdutoRequest
{
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal? Preco { get; set; }
    public string? Categoria { get; set; }
    public int? Estoque { get; set; }
    public string? ContatoEmail { get; set; }
}

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

- [ ] **Step 5: Criar IProdutoService.cs**

Crie `src/Catalogo/Catalogo.Application/Services/IProdutoService.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Shared.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public interface IProdutoService
{
    Task<PaginatedResponse<ProdutoResponse>> ListarProdutosAsync(
        int page, int pageSize, string? categoria = null, string? search = null);
    Task<ProdutoResponse?> ObterProdutoAsync(int id);
    Task<ProdutoResponse> CriarProdutoAsync(CriarProdutoRequest request);
    Task<ProdutoResponse?> AtualizarProdutoAsync(int id, AtualizarProdutoRequest request);
    Task<ProdutoResponse?> AtualizarCompletoProdutoAsync(int id, CriarProdutoRequest request);
    Task<bool> DeletarProdutoAsync(int id);
}
```

- [ ] **Step 6: Criar ProdutoService.cs**

Crie `src/Catalogo/Catalogo.Application/Services/ProdutoService.cs`:

```csharp
using AutoMapper;
using Microsoft.Extensions.Logging;
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Domain;
using FacShopAPI.Shared.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public class ProdutoService : IProdutoService
{
    private readonly IProdutoQueryRepository _queryRepo;
    private readonly IProdutoCommandRepository _commandRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<ProdutoService> _logger;

    public ProdutoService(
        IProdutoQueryRepository queryRepo,
        IProdutoCommandRepository commandRepo,
        IMapper mapper,
        ILogger<ProdutoService> logger)
    {
        _queryRepo = queryRepo;
        _commandRepo = commandRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ProdutoResponse>> ListarProdutosAsync(
        int page, int pageSize, string? categoria = null, string? search = null)
    {
        _logger.LogInformation("Listando produtos - Page: {Page}, PageSize: {PageSize}", page, pageSize);
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (produtos, total) = await _queryRepo.ListarAsync(page, pageSize, categoria, search);
        return new PaginatedResponse<ProdutoResponse>
        {
            Data = produtos.ToList(),
            Pagination = new PaginationInfo
            {
                Page = page, PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        };
    }

    public async Task<ProdutoResponse?> ObterProdutoAsync(int id)
    {
        _logger.LogInformation("Obtendo produto com ID: {ProductId}", id);
        var produto = await _queryRepo.ObterPorIdAsync(id);
        if (produto is null) _logger.LogWarning("Produto {ProductId} nÃ£o encontrado", id);
        return produto;
    }

    public async Task<ProdutoResponse> CriarProdutoAsync(CriarProdutoRequest request)
    {
        _logger.LogInformation("Criando produto: {Nome}", request.Nome);
        var resultado = Produto.Criar(
            request.Nome, request.Descricao, request.Preco,
            request.Categoria, request.Estoque, request.ContatoEmail);
        if (!resultado.IsSuccess) throw new InvalidOperationException(resultado.Error);
        var produto = await _commandRepo.AdicionarAsync(resultado.Value!);
        _logger.LogInformation("Produto criado. ID: {ProductId}", produto.Id);
        return _mapper.Map<ProdutoResponse>(produto);
    }

    public async Task<ProdutoResponse?> AtualizarProdutoAsync(int id, AtualizarProdutoRequest request)
    {
        _logger.LogInformation("Atualizando produto {ProductId}", id);
        var produto = await _commandRepo.ObterPorIdAsync(id);
        if (produto is null) return null;

        if (request.Preco.HasValue)
        {
            var r = produto.AtualizarPreco(request.Preco.Value);
            if (!r.IsSuccess) throw new InvalidOperationException(r.Error);
        }
        if (request.Estoque.HasValue) produto.AjustarEstoque(request.Estoque.Value);

        var r2 = produto.AtualizarDados(request.Nome, request.Descricao, request.Categoria, request.ContatoEmail);
        if (!r2.IsSuccess) throw new InvalidOperationException(r2.Error);
        await _commandRepo.SaveChangesAsync();
        return _mapper.Map<ProdutoResponse>(produto);
    }

    public async Task<ProdutoResponse?> AtualizarCompletoProdutoAsync(int id, CriarProdutoRequest request)
    {
        _logger.LogInformation("Atualizando completamente produto {ProductId}", id);
        var produto = await _commandRepo.ObterPorIdAsync(id);
        if (produto is null) return null;

        if (request.Preco != produto.Preco.Value)
        {
            var r = produto.AtualizarPreco(request.Preco);
            if (!r.IsSuccess) throw new InvalidOperationException(r.Error);
        }
        produto.AjustarEstoque(request.Estoque);
        var r2 = produto.AtualizarDados(request.Nome, request.Descricao, request.Categoria, request.ContatoEmail);
        if (!r2.IsSuccess) throw new InvalidOperationException(r2.Error);
        await _commandRepo.SaveChangesAsync();
        return _mapper.Map<ProdutoResponse>(produto);
    }

    public async Task<bool> DeletarProdutoAsync(int id)
    {
        _logger.LogInformation("Deletando produto {ProductId}", id);
        var deletado = await _commandRepo.DeletarAsync(id);
        if (!deletado) _logger.LogWarning("Produto {ProductId} nÃ£o encontrado para deleÃ§Ã£o", id);
        return deletado;
    }
}
```

- [ ] **Step 7: Criar ProdutoValidator.cs**

Crie `src/Catalogo/Catalogo.Application/Validators/ProdutoValidator.cs`:

```csharp
using FluentValidation;
using FacShopAPI.Catalogo.Application.DTOs.Produto;

namespace FacShopAPI.Catalogo.Application.Validators;

public class CriarProdutoValidator : AbstractValidator<CriarProdutoRequest>
{
    public CriarProdutoValidator()
    {
        RuleFor(p => p.Nome).NotEmpty().WithMessage("Nome Ã© obrigatÃ³rio")
            .MinimumLength(3).WithMessage("Nome deve ter no mÃ­nimo 3 caracteres")
            .MaximumLength(100).WithMessage("Nome nÃ£o pode exceder 100 caracteres");

        RuleFor(p => p.Descricao).NotEmpty().WithMessage("DescriÃ§Ã£o Ã© obrigatÃ³ria")
            .MinimumLength(10).WithMessage("DescriÃ§Ã£o deve ter no mÃ­nimo 10 caracteres")
            .MaximumLength(500).WithMessage("DescriÃ§Ã£o nÃ£o pode exceder 500 caracteres");

        RuleFor(p => p.Preco).GreaterThan(0).WithMessage("PreÃ§o deve ser maior que zero")
            .LessThan(999999.99m).WithMessage("PreÃ§o nÃ£o pode ser tÃ£o alto");

        RuleFor(p => p.Categoria).NotEmpty().WithMessage("Categoria Ã© obrigatÃ³ria")
            .Must(c => new[] { "EletrÃ´nicos", "Livros", "Roupas", "Alimentos", "Outros" }.Contains(c))
            .WithMessage("Categoria invÃ¡lida");

        RuleFor(p => p.Estoque).GreaterThanOrEqualTo(0).WithMessage("Estoque nÃ£o pode ser negativo")
            .LessThan(1000000).WithMessage("Estoque muito alto");

        RuleFor(p => p.ContatoEmail).NotEmpty().WithMessage("Email Ã© obrigatÃ³rio")
            .EmailAddress().WithMessage("Email invÃ¡lido");
    }
}

public class AtualizarProdutoValidator : AbstractValidator<AtualizarProdutoRequest>
{
    public AtualizarProdutoValidator()
    {
        RuleFor(p => p.Nome).MinimumLength(3).WithMessage("Nome deve ter no mÃ­nimo 3 caracteres")
            .MaximumLength(100).When(p => !string.IsNullOrEmpty(p.Nome));

        RuleFor(p => p.Descricao).MinimumLength(10).WithMessage("DescriÃ§Ã£o deve ter no mÃ­nimo 10 caracteres")
            .MaximumLength(500).When(p => !string.IsNullOrEmpty(p.Descricao));

        RuleFor(p => p.Preco).GreaterThan(0).WithMessage("PreÃ§o deve ser maior que zero")
            .When(p => p.Preco.HasValue);

        RuleFor(p => p.Categoria)
            .Must(c => new[] { "EletrÃ´nicos", "Livros", "Roupas", "Alimentos", "Outros" }.Contains(c))
            .WithMessage("Categoria invÃ¡lida").When(p => !string.IsNullOrEmpty(p.Categoria));

        RuleFor(p => p.Estoque).GreaterThanOrEqualTo(0).WithMessage("Estoque nÃ£o pode ser negativo")
            .When(p => p.Estoque.HasValue);

        RuleFor(p => p.ContatoEmail).EmailAddress().WithMessage("Email invÃ¡lido")
            .When(p => !string.IsNullOrEmpty(p.ContatoEmail));
    }
}
```

- [ ] **Step 8: Criar ProdutoMappingProfile.cs**

Crie `src/Catalogo/Catalogo.Application/Mappings/ProdutoMappingProfile.cs`:

```csharp
using AutoMapper;
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Mappings;

public class ProdutoMappingProfile : Profile
{
    public ProdutoMappingProfile()
    {
        CreateMap<Produto, ProdutoResponse>()
            .ForMember(dest => dest.Descricao, opt => opt.MapFrom(src => src.Descricao.Value))
            .ForMember(dest => dest.Preco, opt => opt.MapFrom(src => src.Preco.Value))
            .ForMember(dest => dest.Categoria, opt => opt.MapFrom(src => src.Categoria.Value))
            .ForMember(dest => dest.Estoque, opt => opt.MapFrom(src => src.Estoque.Value));
    }
}
```

- [ ] **Step 9: Build Catalogo.Application**

```bash
dotnet build src/Catalogo/Catalogo.Application/Catalogo.Application.csproj
```

Esperado: `Build succeeded, 0 errors`.

---

### Task 4: Criar Catalogo.Infrastructure

**Files:**

- Create: `src/Catalogo/Catalogo.Infrastructure/Repositories/EfProdutoCommandRepository.cs`
- Create: `src/Catalogo/Catalogo.Infrastructure/Queries/DapperProdutoQueryRepository.cs`
- Create: `src/Catalogo/Catalogo.Infrastructure/Data/DbSeeder.cs`

- [ ] **Step 1: Criar EfProdutoCommandRepository.cs**

Crie `src/Catalogo/Catalogo.Infrastructure/Repositories/EfProdutoCommandRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Infrastructure.Repositories;

public class EfProdutoCommandRepository(ICatalogoContext context) : IProdutoCommandRepository
{
    public Task<Produto?> ObterPorIdAsync(int id) =>
        context.Produtos.FirstOrDefaultAsync(p => p.Id == id && p.Ativo);

    public async Task<Produto> AdicionarAsync(Produto produto)
    {
        context.AddProduto(produto);
        await context.SaveChangesAsync();
        return produto;
    }

    public async Task<bool> DeletarAsync(int id)
    {
        var produto = await context.Produtos.FirstOrDefaultAsync(p => p.Id == id && p.Ativo);
        if (produto is null) return false;
        produto.Desativar();
        await context.SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();
}
```

- [ ] **Step 2: Criar DapperProdutoQueryRepository.cs**

Crie `src/Catalogo/Catalogo.Infrastructure/Queries/DapperProdutoQueryRepository.cs`:

```csharp
using System.Data;
using System.Data.Common;
using System.Text;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;

namespace FacShopAPI.Catalogo.Infrastructure.Queries;

public class DapperProdutoQueryRepository : IProdutoQueryRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<DapperProdutoQueryRepository> _logger;

    public DapperProdutoQueryRepository(ICatalogoContext context, ILogger<DapperProdutoQueryRepository> logger)
    {
        _dbContext = context as DbContext
            ?? throw new InvalidOperationException(
                "ICatalogoContext precisa ser uma implementaÃ§Ã£o de DbContext para suportar Dapper.");
        _logger = logger;
    }

    public Task<(IReadOnlyList<ProdutoResponse> Items, int Total)> ListarAsync(
        int page, int pageSize, string? categoria = null, string? search = null) =>
        WithConnectionAsync(async connection =>
        {
            var where = new StringBuilder("WHERE Ativo = 1");
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                where.Append(" AND Categoria = @Categoria");
                parameters.Add("Categoria", categoria);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                where.Append(" AND (Nome LIKE @Search OR Descricao LIKE @Search)");
                parameters.Add("Search", $"%{search}%");
            }

            parameters.Add("Limit", pageSize);
            parameters.Add("Offset", (page - 1) * pageSize);

            var totalSql = $"SELECT COUNT(1) FROM Produtos {where};";
            var itemsSql = $@"
SELECT Id, Nome, Descricao, Preco, Categoria, Estoque, Ativo, ContatoEmail, DataCriacao, DataAtualizacao
FROM Produtos {where}
ORDER BY DataCriacao DESC
LIMIT @Limit OFFSET @Offset;";

            var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(totalSql, parameters));
            var rows = await connection.QueryAsync<ProdutoRow>(new CommandDefinition(itemsSql, parameters));
            return ((IReadOnlyList<ProdutoResponse>)rows.Select(MapToDto).ToList(), total);
        });

    public Task<ProdutoResponse?> ObterPorIdAsync(int id) =>
        WithConnectionAsync(async connection =>
        {
            const string sql = @"
SELECT Id, Nome, Descricao, Preco, Categoria, Estoque, Ativo, ContatoEmail, DataCriacao, DataAtualizacao
FROM Produtos WHERE Id = @Id AND Ativo = 1;";
            var row = await connection.QuerySingleOrDefaultAsync<ProdutoRow>(
                new CommandDefinition(sql, new { Id = id }));
            return row is null ? null : MapToDto(row);
        });

    private async Task<T> WithConnectionAsync<T>(Func<DbConnection, Task<T>> action)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync();
        try { return await action(connection); }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private static ProdutoResponse MapToDto(ProdutoRow row) => new()
    {
        Id = row.Id, Nome = row.Nome, Descricao = row.Descricao, Preco = row.Preco,
        Categoria = row.Categoria, Estoque = row.Estoque, Ativo = row.Ativo,
        ContatoEmail = row.ContatoEmail, DataCriacao = row.DataCriacao, DataAtualizacao = row.DataAtualizacao
    };

    private sealed class ProdutoRow
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
}
```

- [ ] **Step 3: Criar DbSeeder.cs**

Crie `src/Catalogo/Catalogo.Infrastructure/Data/DbSeeder.cs`:

```csharp
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Infrastructure.Data;

public static class DbSeeder
{
    public static void Seed(ICatalogoContext context)
    {
        if (context.Produtos.Any()) return;

        var produtos = new[]
        {
            Produto.Criar("Notebook Dell XPS 13", "Notebook de alta performance com processador Intel Core i7, 16GB RAM e 512GB SSD", 4500.00m, "EletrÃ´nicos", 5, "vendas@dell.com").Value!,
            Produto.Criar("Mouse Logitech MX Master 3S", "Mouse wireless de precisÃ£o profissional com mÃºltiplos botÃµes e rastreamento avanÃ§ado", 450.00m, "EletrÃ´nicos", 25, "suporte@logitech.com").Value!,
            Produto.Criar("Teclado MecÃ¢nico RGB", "Teclado mecÃ¢nico com iluminaÃ§Ã£o RGB, switches Cherry MX e design compacto", 350.00m, "EletrÃ´nicos", 15, "contato@keyboards.com.br").Value!,
            Produto.Criar("Clean Code", "Guia prÃ¡tico para escrever cÃ³digo limpo e manutenÃ­vel. Essencial para todo desenvolvedor", 89.90m, "Livros", 30, "vendas@books.com").Value!,
            Produto.Criar("Design Patterns", "PadrÃµes de design reutilizÃ¡veis para desenvolvimento de software. ReferÃªncia obrigatÃ³ria", 75.00m, "Livros", 20, "vendas@books.com").Value!,
            Produto.Criar("Camiseta tÃ©cnica Azul", "Camiseta de poliÃ©ster com tecnologia anti-transpiraÃ§Ã£o, disponÃ­vel em vÃ¡rios tamanhos", 79.90m, "Roupas", 50, "vendas@clothing.com.br").Value!,
            Produto.Criar("CafÃ© Gourmet 500g", "CafÃ© gourmet especial com grÃ£os selecionados de plantaÃ§Ãµes premium da regiÃ£o", 45.00m, "Alimentos", 100, "vendas@coffee.com.br").Value!,
            Produto.Criar("Monitor LG UltraWide 34\"", "Monitor curvo ultrawide com resoluÃ§Ã£o 3440x1440, ideal para produtividade e games", 1899.00m, "EletrÃ´nicos", 3, "suporte@lg.com.br").Value!
        };

        foreach (var p in produtos) context.AddProduto(p);
        context.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
```

- [ ] **Step 4: Build Catalogo.Infrastructure**

```bash
dotnet build src/Catalogo/Catalogo.Infrastructure/Catalogo.Infrastructure.csproj
```

Esperado: `Build succeeded, 0 errors`.

---

### Task 5: Criar Catalogo.API

**Files:**

- Create: `src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs`
- Create: `src/Catalogo/Catalogo.API/Endpoints/Auth/AuthEndpoints.cs`
- Create: `src/Catalogo/Catalogo.API/Extensions/CatalogoServiceExtensions.cs`
- Create: `src/Catalogo/Catalogo.API/GlobalUsings.cs`

- [ ] **Step 1: Criar GlobalUsings.cs**

Crie `src/Catalogo/Catalogo.API/GlobalUsings.cs`:

```csharp
global using Microsoft.AspNetCore.Builder;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
```

- [ ] **Step 2: Criar ProdutoEndpoints.cs**

A assinatura muda: recebe `RouteGroupBuilder` em vez de `WebApplication`, e usa path relativo `/produtos`.

Crie `src/Catalogo/Catalogo.API/Endpoints/Produtos/ProdutoEndpoints.cs`:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Catalogo.Application.Services;
using FacShopAPI.Shared.Common;

namespace FacShopAPI.Catalogo.API.Endpoints.Produtos;

public static class ProdutoEndpoints
{
    public static void MapProdutoEndpoints(this RouteGroupBuilder catalogoGroup)
    {
        var group = catalogoGroup.MapGroup("/produtos")
            .WithTags("CatÃ¡logo - Produtos");

        group.MapGet("/", ListarProdutos).WithName("ListarProdutos")
            .Produces<PaginatedResponse<ProdutoResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous();

        group.MapGet("/{id:int}", ObterProduto).WithName("ObterProduto")
            .Produces<ProdutoResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        group.MapPost("/", CriarProduto).WithName("CriarProduto")
            .Accepts<CriarProdutoRequest>("application/json")
            .Produces<ProdutoResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        group.MapPut("/{id:int}", AtualizarCompletoProduto).WithName("AtualizarCompletoProduto")
            .Accepts<CriarProdutoRequest>("application/json")
            .Produces<ProdutoResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapPatch("/{id:int}", AtualizarParcialProduto).WithName("AtualizarParcialProduto")
            .Accepts<AtualizarProdutoRequest>("application/json")
            .Produces<ProdutoResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapDelete("/{id:int}", DeletarProduto).WithName("DeletarProduto")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    private static async Task<IResult> ListarProdutos(
        IProdutoService service, int page = 1, int pageSize = 20,
        string? categoria = null, string? search = null)
    {
        var resultado = await service.ListarProdutosAsync(page, pageSize, categoria, search);
        return Results.Ok(resultado);
    }

    private static async Task<IResult> ObterProduto(int id, IProdutoService service)
    {
        var produto = await service.ObterProdutoAsync(id);
        if (produto is null)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404, Title = "Produto nÃ£o encontrado",
                Detail = $"Produto com ID {id} nÃ£o encontrado.",
                Type = "https://api.example.com/errors/not-found",
                Instance = $"/api/v1/catalogo/produtos/{id}"
            });
        return Results.Ok(produto);
    }

    private static async Task<IResult> CriarProduto(
        CriarProdutoRequest request, IProdutoService service,
        IValidator<CriarProdutoRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422, Title = "ValidaÃ§Ã£o falhou",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                Type = "https://api.example.com/errors/validation"
            });
        var produto = await service.CriarProdutoAsync(request);
        return Results.Created($"/api/v1/catalogo/produtos/{produto.Id}", produto);
    }

    private static async Task<IResult> AtualizarCompletoProduto(
        int id, CriarProdutoRequest request, IProdutoService service,
        IValidator<CriarProdutoRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422, Title = "ValidaÃ§Ã£o falhou",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                Type = "https://api.example.com/errors/validation"
            });
        var produto = await service.AtualizarCompletoProdutoAsync(id, request);
        if (produto is null)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404, Title = "Produto nÃ£o encontrado",
                Detail = $"Produto com ID {id} nÃ£o encontrado.",
                Type = "https://api.example.com/errors/not-found",
                Instance = $"/api/v1/catalogo/produtos/{id}"
            });
        return Results.Ok(produto);
    }

    private static async Task<IResult> AtualizarParcialProduto(
        int id, AtualizarProdutoRequest request, IProdutoService service,
        IValidator<AtualizarProdutoRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422, Title = "ValidaÃ§Ã£o falhou",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                Type = "https://api.example.com/errors/validation"
            });
        var produto = await service.AtualizarProdutoAsync(id, request);
        if (produto is null)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404, Title = "Produto nÃ£o encontrado",
                Detail = $"Produto com ID {id} nÃ£o encontrado.",
                Type = "https://api.example.com/errors/not-found",
                Instance = $"/api/v1/catalogo/produtos/{id}"
            });
        return Results.Ok(produto);
    }

    private static async Task<IResult> DeletarProduto(int id, IProdutoService service)
    {
        var deletado = await service.DeletarProdutoAsync(id);
        if (!deletado)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404, Title = "Produto nÃ£o encontrado",
                Detail = $"Produto com ID {id} nÃ£o encontrado.",
                Type = "https://api.example.com/errors/not-found",
                Instance = $"/api/v1/catalogo/produtos/{id}"
            });
        return Results.NoContent();
    }
}
```

- [ ] **Step 3: Criar AuthEndpoints.cs**

Crie `src/Catalogo/Catalogo.API/Endpoints/Auth/AuthEndpoints.cs` â€” mesma lÃ³gica, apenas namespace atualizado:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using FacShopAPI.Shared.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FacShopAPI.Catalogo.API.Endpoints.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/login", Login).WithName("UserLogin")
            .Accepts<LoginRequest>("application/json")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();
    }

    private static IResult Login(LoginRequest req, IConfiguration configuration)
    {
        var adminEmail = configuration["Auth:AdminEmail"];
        var adminPassword = configuration["Auth:AdminPassword"];
        if (req.Email != adminEmail || req.Senha != adminPassword)
            return Results.Unauthorized();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "admin_id"),
            new Claim(JwtRegisteredClaimNames.Email, req.Email),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var secretKey = configuration["Jwt:Key"] ?? "MinhaChaveSuperSecretaDePeloMenos32BytesAki123!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "FacShopAPI",
            audience: configuration["Jwt:Audience"] ?? "TodosOsClientes",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return Results.Ok(new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresIn = (int)TimeSpan.FromHours(2).TotalSeconds
        });
    }
}
```

- [ ] **Step 4: Criar CatalogoServiceExtensions.cs**

Crie `src/Catalogo/Catalogo.API/Extensions/CatalogoServiceExtensions.cs`:

```csharp
using FluentValidation;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Application.Services;
using FacShopAPI.Catalogo.Application.Validators;
using FacShopAPI.Catalogo.Infrastructure.Queries;
using FacShopAPI.Catalogo.Infrastructure.Repositories;

namespace FacShopAPI.Catalogo.API.Extensions;

public static class CatalogoServiceExtensions
{
    public static IServiceCollection AddCatalogo(this IServiceCollection services)
    {
        services.AddScoped<IProdutoService, ProdutoService>();
        services.AddScoped<IProdutoQueryRepository, DapperProdutoQueryRepository>();
        services.AddScoped<IProdutoCommandRepository, EfProdutoCommandRepository>();
        services.AddValidatorsFromAssemblyContaining<CriarProdutoValidator>();
        return services;
    }
}
```

- [ ] **Step 5: Build Catalogo.API**

```bash
dotnet build src/Catalogo/Catalogo.API/Catalogo.API.csproj
```

Esperado: `Build succeeded, 0 errors`.

---

### Task 6: Atualizar AppDbContext, Program.cs, csproj e slnx

**Files:**

- Modify: `src/Shared/Data/AppDbContext.cs`
- Modify: `src/Shared/Common/PaginatedResponse.cs` (ou criar se nÃ£o existir)
- Modify: `Program.cs`
- Modify: `FacShopAPI.csproj`
- Modify: `FacShopAPI.slnx`

- [ ] **Step 1: Verificar se PaginatedResponse existe em Shared**

```bash
grep -r "PaginatedResponse" src/Shared/
```

Se nÃ£o existir, crie `src/Shared/Common/PaginatedResponse.cs`:

```csharp
namespace FacShopAPI.Shared.Common;

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

public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public Dictionary<string, List<string>> Errors { get; set; } = new();
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}
```

> **Nota:** `PaginatedResponse<T>`, `ErrorResponse`, `AuthResponse`, `LoginRequest` precisam estar em `FacShopAPI.Shared.Common` para que tanto `Catalogo.Application` quanto `Catalogo.API` possam usÃ¡-los. Adicione referÃªncia ao projeto principal em `Catalogo.Application.csproj` e `Catalogo.API.csproj`:
>
> ```xml
> <!-- Adicionar em Catalogo.Application.csproj e Catalogo.API.csproj -->
> <ItemGroup>
>   <ProjectReference Include="../../../src/Shared/..." />
> </ItemGroup>
> ```
>
> **Alternativa mais simples:** Mover `PaginatedResponse`, `ErrorResponse`, `AuthResponse`, `LoginRequest` para dentro de `Catalogo.Application/DTOs/` e `Catalogo.API/` respectivamente, evitando dependÃªncia circular.
>
> **DecisÃ£o recomendada:** Mantenha `ErrorResponse` e `PaginatedResponse` em `Catalogo.Application/DTOs/Common/`:

Crie `src/Catalogo/Catalogo.Application/DTOs/Common/CommonDTOs.cs`:

```csharp
namespace FacShopAPI.Catalogo.Application.DTOs.Common;

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

Crie `src/Catalogo/Catalogo.API/DTOs/CommonApiDTOs.cs`:

```csharp
namespace FacShopAPI.Catalogo.API.DTOs;

public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public Dictionary<string, List<string>> Errors { get; set; } = new();
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}
```

> Atualize todos os `using` nos arquivos de Task 3â€“5 que referenciam `FacShopAPI.Shared.Common` para `FacShopAPI.Catalogo.Application.DTOs.Common` (para PaginatedResponse) e `FacShopAPI.Catalogo.API.DTOs` (para ErrorResponse/AuthResponse/LoginRequest).

- [ ] **Step 2: Atualizar AppDbContext**

Em `src/Shared/Data/AppDbContext.cs`, substitua `IProdutoContext` por `ICatalogoContext`:

```csharp
// Remover:
using FacShopAPI.Produtos.Application.Interfaces;
using FacShopAPI.Produtos.Domain;
using FacShopAPI.Produtos.Domain.ValueObjects;

// Adicionar:
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Domain;
using FacShopAPI.Catalogo.Domain.ValueObjects;
```

DeclaraÃ§Ã£o da classe:

```csharp
public class AppDbContext : DbContext, ICatalogoContext
```

Propriedades:

```csharp
public DbSet<Produto> Produtos => Set<Produto>();
IQueryable<Produto> ICatalogoContext.Produtos => Set<Produto>();
public void AddProduto(Produto produto) => this.Add(produto);
```

O `OnModelCreating` mantÃ©m a mesma configuraÃ§Ã£o (apenas os `using` de ValueObjects mudam para `FacShopAPI.Catalogo.Domain.ValueObjects`).

- [ ] **Step 3: Atualizar Program.cs**

Substitua os `using` e as chamadas:

```csharp
// Remover:
using FacShopAPI.Produtos.API.Endpoints;
using FacShopAPI.Produtos.API.Extensions;
using FacShopAPI.Produtos.Application.Interfaces;
using FacShopAPI.Produtos.Application.Mappings;
using FacShopAPI.Produtos.Infrastructure.Data;

// Adicionar:
using FacShopAPI.Catalogo.API.Endpoints.Auth;
using FacShopAPI.Catalogo.API.Endpoints.Produtos;
using FacShopAPI.Catalogo.API.Extensions;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Mappings;
using FacShopAPI.Catalogo.Infrastructure.Data;
```

Substituir no DI (apÃ³s `builder.Services.AddDbContext`):

```csharp
// Remover:
builder.Services.AddScoped<IProdutoContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddProdutos();

// Adicionar:
builder.Services.AddScoped<ICatalogoContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddCatalogo();
```

Substituir no AutoMapper:

```csharp
// Remover:
builder.Services.AddAutoMapper(_ => { }, typeof(ProdutoMappingProfile).Assembly);

// Adicionar:
builder.Services.AddAutoMapper(_ => { }, typeof(FacShopAPI.Catalogo.Application.Mappings.ProdutoMappingProfile).Assembly);
```

Substituir no seed:

```csharp
// Remover:
DbSeeder.Seed(dbContext);

// Adicionar:
FacShopAPI.Catalogo.Infrastructure.Data.DbSeeder.Seed(dbContext);
```

Substituir mapeamento de endpoints:

```csharp
// Remover:
app.MapAuthEndpoints();
app.MapProdutoEndpoints();

// Adicionar:
app.MapAuthEndpoints();  // mÃ©todo de Catalogo.API.Endpoints.Auth.AuthEndpoints

var v1 = app.MapGroup("/api/v1");
var catalogo = v1.MapGroup("/catalogo");
catalogo.MapProdutoEndpoints();  // mÃ©todo de Catalogo.API.Endpoints.Produtos.ProdutoEndpoints
```

- [ ] **Step 4: Atualizar FacShopAPI.csproj**

Substitua as ProjectReferences de Produtos pelas de Catalogo:

```xml
<!-- Remover: -->
<Compile Remove="src/Produtos/Produtos.Domain/**" />
<Compile Remove="src/Produtos/Produtos.Application/**" />
<Compile Remove="src/Produtos/Produtos.Infrastructure/**" />
<Compile Remove="src/Produtos/Produtos.API/**" />

<!-- Adicionar: -->
<Compile Remove="src/Catalogo/Catalogo.Domain/**" />
<Compile Remove="src/Catalogo/Catalogo.Application/**" />
<Compile Remove="src/Catalogo/Catalogo.Infrastructure/**" />
<Compile Remove="src/Catalogo/Catalogo.API/**" />
```

```xml
<!-- Remover: -->
<ProjectReference Include="src/Produtos/Produtos.API/Produtos.API.csproj" />
<ProjectReference Include="src/Produtos/Produtos.Infrastructure/Produtos.Infrastructure.csproj" />

<!-- Adicionar: -->
<ProjectReference Include="src/Catalogo/Catalogo.API/Catalogo.API.csproj" />
<ProjectReference Include="src/Catalogo/Catalogo.Infrastructure/Catalogo.Infrastructure.csproj" />
```

- [ ] **Step 5: Atualizar FacShopAPI.slnx**

```xml
<Solution>
  <Project Path="FacShopAPI.csproj" />
  <Project Path="src/Catalogo/Catalogo.Domain/Catalogo.Domain.csproj" />
  <Project Path="src/Catalogo/Catalogo.Application/Catalogo.Application.csproj" />
  <Project Path="src/Catalogo/Catalogo.Infrastructure/Catalogo.Infrastructure.csproj" />
  <Project Path="src/Catalogo/Catalogo.API/Catalogo.API.csproj" />
  <Project Path="src/Pix/Pix.MockServer/Pix.MockServer.csproj" />
  <Project Path="src/Pix/Pix.ClientDemo/Pix.ClientDemo.csproj" />
  <Project Path="tests/FacShopAPI.Tests/FacShopAPI.Tests.csproj" />
  <Project Path="tests/Pedidos.Tests/Pedidos.Tests.csproj" />
  <Project Path="tests/Pix.MockServer.Tests/Pix.MockServer.Tests.csproj" />
</Solution>
```

- [ ] **Step 6: Atualizar FacShopAPI.Tests.csproj**

```xml
<!-- Substituir ProjectReferences: -->
<ProjectReference Include="../../FacShopAPI.csproj" />
<ProjectReference Include="../../src/Catalogo/Catalogo.Domain/Catalogo.Domain.csproj" />
<ProjectReference Include="../../src/Catalogo/Catalogo.Application/Catalogo.Application.csproj" />
<ProjectReference Include="../../src/Catalogo/Catalogo.Infrastructure/Catalogo.Infrastructure.csproj" />
```

- [ ] **Step 7: Build do projeto principal**

```bash
dotnet build FacShopAPI.csproj
```

Esperado: `Build succeeded, 0 errors`.

---

### Task 7: Atualizar testes para novos namespaces e rotas

**Files:**

- Modify: `tests/FacShopAPI.Tests/Integration/ApiFactory.cs`
- Modify: `tests/FacShopAPI.Tests/Endpoints/ProdutoEndpointsTests.cs`
- Modify: `tests/FacShopAPI.Tests/Services/ProdutoServiceTests.cs`
- Modify: `tests/FacShopAPI.Tests/Validators/ProdutoValidatorTests.cs`
- Modify: `tests/FacShopAPI.Tests/Unit/Domain/ProdutoTests.cs`
- Modify: `tests/FacShopAPI.Tests/Builders/ProdutoBuilder.cs`

- [ ] **Step 1: Atualizar ApiFactory.cs**

```csharp
using FacShopAPI.Catalogo.Infrastructure.Data;  // era Produtos.Infrastructure.Data
// resto igual â€” DbSeeder.Seed(db) continua funcionando com novo namespace
```

- [ ] **Step 2: Atualizar usando em ProdutoEndpointsTests.cs**

Substituir em todos os arquivos de teste:

```csharp
// Remover:
using FacShopAPI.Produtos.Application.DTOs;
using FacShopAPI.Produtos.Domain;
using FacShopAPI.Produtos.Domain.Common;

// Adicionar:
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Catalogo.Application.DTOs.Common;
using FacShopAPI.Catalogo.Domain;
using FacShopAPI.Catalogo.Domain.Common;
```

Substituir todas as rotas nos testes:

```csharp
// PadrÃ£o de busca e substituiÃ§Ã£o:
"/api/v1/produtos"  â†’  "/api/v1/catalogo/produtos"
```

Execute em bash para confirmar quais arquivos tÃªm a rota antiga:

```bash
grep -rn "/api/v1/produtos" tests/FacShopAPI.Tests/
```

- [ ] **Step 3: Atualizar ProdutoServiceTests.cs**

```csharp
// Remover:
using FacShopAPI.Produtos.Application.Repositories;
using FacShopAPI.Produtos.Application.Services;
using FacShopAPI.Produtos.Application.DTOs;
using FacShopAPI.Produtos.Domain;

// Adicionar:
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Application.Services;
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Catalogo.Domain;
```

- [ ] **Step 4: Atualizar ProdutoValidatorTests.cs e ProdutoTests.cs e ProdutoBuilder.cs**

Mesmo padrÃ£o: substituir `FacShopAPI.Produtos.*` por `FacShopAPI.Catalogo.*` em todos os `using`.

- [ ] **Step 5: Rodar todos os testes**

```bash
dotnet test tests/FacShopAPI.Tests/FacShopAPI.Tests.csproj --no-build
```

Esperado: todos os testes passando (mesmo nÃºmero de antes).

---

### Task 8: Deletar Produtos sub-projetos e commit final da Fase 1

- [ ] **Step 1: Confirmar que build e testes passam**

```bash
dotnet build FacShopAPI.slnx
dotnet test FacShopAPI.slnx
```

Esperado: 0 erros de build, todos os testes passando.

- [ ] **Step 2: Deletar src/Produtos/**

```bash
rm -rf src/Produtos/
```

- [ ] **Step 3: Build novamente para confirmar que nada quebrou**

```bash
dotnet build FacShopAPI.slnx
dotnet test tests/FacShopAPI.Tests/FacShopAPI.Tests.csproj
```

Esperado: 0 erros, todos os testes passando.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor: migrar Produtos para Catalogo com versionamento /api/v1/catalogo/*

- RenomeaÃ§Ã£o de src/Produtos/ â†’ src/Catalogo/ com 4 sub-projetos
- Namespaces FacShopAPI.Produtos.* â†’ FacShopAPI.Catalogo.*
- IProdutoContext â†’ ICatalogoContext
- Rota /api/v1/produtos â†’ /api/v1/catalogo/produtos via route groups
- AddProdutos() â†’ AddCatalogo() no DI
- Testes atualizados com novos namespaces e rotas"
```
