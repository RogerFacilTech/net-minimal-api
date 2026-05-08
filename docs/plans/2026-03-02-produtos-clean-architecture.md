# Produtos Clean Architecture Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrar o feature Produtos para arquitetura limpa em 4 sub-projetos (`Produtos.Domain`, `Produtos.Application`, `Produtos.Infrastructure`, `Produtos.API`) dentro de `src/Produtos/`, mantendo o projeto `FacShopAPI.csproj` como host e sem quebrar os testes existentes.

**Architecture:** Domain sem dependÃªncias externas; Application depende de Domain e define abstraÃ§Ãµes (interfaces); Infrastructure implementa repositÃ³rio com EF Core via interface definida em Application; API contÃ©m endpoints e wiring de DI. O `AppDbContext` permanece no projeto principal (cross-cutting entre Produtos e Pedidos) e implementa a interface `IProdutoContext` definida em Application. O fluxo de dependÃªncias Ã©: `API â†’ Application â† Infrastructure` e `API â†’ Infrastructure` (para DI).

**Tech Stack:** .NET 10, xUnit, FluentAssertions, EF Core 10, AutoMapper 12, FluentValidation 11, Microsoft.AspNetCore.App (FrameworkReference).

---

## Mapa de MudanÃ§as de Namespace

| Antes                                      | Depois                                                             |
| ------------------------------------------ | ------------------------------------------------------------------ |
| `FacShopAPI.Produtos.Models.Produto`      | `FacShopAPI.Produtos.Domain.Produto`                              |
| `FacShopAPI.Shared.Common.MappingProfile` | `FacShopAPI.Produtos.Application.Mappings.ProdutoMappingProfile`  |
| `FacShopAPI.Produtos.DTOs.*`              | `FacShopAPI.Produtos.Application.DTOs.*` (mesmos nomes de classe) |
| `FacShopAPI.Produtos.Services.*`          | `FacShopAPI.Produtos.Application.Services.*` (mesmos nomes)       |
| `FacShopAPI.Produtos.Validators.*`        | `FacShopAPI.Produtos.Application.Validators.*`                    |
| `FacShopAPI.Produtos.Endpoints.*`         | `FacShopAPI.Produtos.API.Endpoints.*`                             |
| `FacShopAPI.Shared.Data.DbSeeder`         | `FacShopAPI.Produtos.Infrastructure.Data.DbSeeder`                |

---

## Estrutura de Destino

```
src/Produtos/
â”œâ”€â”€ Produtos.Domain/
â”‚   â”œâ”€â”€ Produtos.Domain.csproj
â”‚   â”œâ”€â”€ Produto.cs
â”‚   â””â”€â”€ Common/
â”‚       â””â”€â”€ Result.cs
â”œâ”€â”€ Produtos.Application/
â”‚   â”œâ”€â”€ Produtos.Application.csproj
â”‚   â”œâ”€â”€ DTOs/
â”‚   â”‚   â””â”€â”€ ProdutoDTO.cs
â”‚   â”œâ”€â”€ Interfaces/
â”‚   â”‚   â””â”€â”€ IProdutoContext.cs
â”‚   â”œâ”€â”€ Repositories/
â”‚   â”‚   â””â”€â”€ IProdutoRepository.cs
â”‚   â”œâ”€â”€ Services/
â”‚   â”‚   â”œâ”€â”€ IProdutoService.cs
â”‚   â”‚   â””â”€â”€ ProdutoService.cs
â”‚   â”œâ”€â”€ Validators/
â”‚   â”‚   â””â”€â”€ ProdutoValidator.cs
â”‚   â””â”€â”€ Mappings/
â”‚       â””â”€â”€ ProdutoMappingProfile.cs
â”œâ”€â”€ Produtos.Infrastructure/
â”‚   â”œâ”€â”€ Produtos.Infrastructure.csproj
â”‚   â”œâ”€â”€ Repositories/
â”‚   â”‚   â””â”€â”€ EfProdutoRepository.cs
â”‚   â””â”€â”€ Data/
â”‚       â””â”€â”€ DbSeeder.cs
â””â”€â”€ Produtos.API/
    â”œâ”€â”€ Produtos.API.csproj
    â”œâ”€â”€ Endpoints/
    â”‚   â”œâ”€â”€ ProdutoEndpoints.cs
    â”‚   â””â”€â”€ AuthEndpoints.cs
    â””â”€â”€ Extensions/
        â””â”€â”€ ProdutosServiceExtensions.cs
```

---

## Task 1: Criar estrutura de diretÃ³rios e arquivos de projeto (.csproj)

**Files:**

- Create: `src/Produtos/Produtos.Domain/Produtos.Domain.csproj`
- Create: `src/Produtos/Produtos.Application/Produtos.Application.csproj`
- Create: `src/Produtos/Produtos.Infrastructure/Produtos.Infrastructure.csproj`
- Create: `src/Produtos/Produtos.API/Produtos.API.csproj`

**Step 1: Criar diretÃ³rios**

```bash
mkdir -p src/Produtos/Produtos.Domain/Common
mkdir -p src/Produtos/Produtos.Application/DTOs
mkdir -p src/Produtos/Produtos.Application/Interfaces
mkdir -p src/Produtos/Produtos.Application/Repositories
mkdir -p src/Produtos/Produtos.Application/Services
mkdir -p src/Produtos/Produtos.Application/Validators
mkdir -p src/Produtos/Produtos.Application/Mappings
mkdir -p src/Produtos/Produtos.Infrastructure/Repositories
mkdir -p src/Produtos/Produtos.Infrastructure/Data
mkdir -p src/Produtos/Produtos.API/Endpoints
mkdir -p src/Produtos/Produtos.API/Extensions
```

**Step 2: Criar Produtos.Domain.csproj**

Crie o arquivo `src/Produtos/Produtos.Domain/Produtos.Domain.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>FacShopAPI.Produtos.Domain</RootNamespace>
    <AssemblyName>Produtos.Domain</AssemblyName>
  </PropertyGroup>
</Project>
```

**Step 3: Criar Produtos.Application.csproj**

Crie o arquivo `src/Produtos/Produtos.Application/Produtos.Application.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>FacShopAPI.Produtos.Application</RootNamespace>
    <AssemblyName>Produtos.Application</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../Produtos.Domain/Produtos.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="AutoMapper" Version="12.0.1" />
    <PackageReference Include="FluentValidation" Version="11.10.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
  </ItemGroup>
</Project>
```

**Step 4: Criar Produtos.Infrastructure.csproj**

Crie o arquivo `src/Produtos/Produtos.Infrastructure/Produtos.Infrastructure.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>FacShopAPI.Produtos.Infrastructure</RootNamespace>
    <AssemblyName>Produtos.Infrastructure</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../Produtos.Application/Produtos.Application.csproj" />
    <ProjectReference Include="../Produtos.Domain/Produtos.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
  </ItemGroup>
</Project>
```

**Step 5: Criar Produtos.API.csproj**

Crie o arquivo `src/Produtos/Produtos.API/Produtos.API.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>FacShopAPI.Produtos.API</RootNamespace>
    <AssemblyName>Produtos.API</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../Produtos.Application/Produtos.Application.csproj" />
    <ProjectReference Include="../Produtos.Infrastructure/Produtos.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="11.10.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.1" />
  </ItemGroup>
</Project>
```

**Step 6: Verificar que os arquivos existem**

```bash
ls src/Produtos/Produtos.Domain/
ls src/Produtos/Produtos.Application/
ls src/Produtos/Produtos.Infrastructure/
ls src/Produtos/Produtos.API/
```

Esperado: cada diretÃ³rio deve conter o `.csproj` e subdiretÃ³rios.

---

## Task 2: Criar Produtos.Domain â€” Produto.cs e Result.cs

**Files:**

- Create: `src/Produtos/Produtos.Domain/Common/Result.cs`
- Create: `src/Produtos/Produtos.Domain/Produto.cs`
- Source: `src/Produtos/Models/Produto.cs` (referÃªncia â€” nÃ£o apagar ainda)

**Step 1: Escrever o teste que verifica compilaÃ§Ã£o da entidade**

O teste jÃ¡ existe em `FacShopAPI.Tests/Unit/Domain/ProdutoTests.cs`. Depois de migrar o namespace, ele deve continuar passando. Rodar os testes agora para ver o estado atual:

```bash
dotnet test FacShopAPI.Tests/FacShopAPI.Tests.csproj --filter "FullyQualifiedName~ProdutoTests" --no-build
```

Esperado: PASS (estado atual, antes da migraÃ§Ã£o).

**Step 2: Criar Result.cs em Domain**

Crie `src/Produtos/Produtos.Domain/Common/Result.cs`:

```csharp
namespace FacShopAPI.Produtos.Domain.Common;

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

**Step 3: Criar Produto.cs em Domain**

Crie `src/Produtos/Produtos.Domain/Produto.cs`. Copie o conteÃºdo de `src/Produtos/Models/Produto.cs` e ajuste:

- Namespace: `FacShopAPI.Produtos.Domain`
- Using: troque `using FacShopAPI.Shared.Common;` por `using FacShopAPI.Produtos.Domain.Common;`
- Torne `AjustarEstoque` `public` (nÃ£o mais `internal`, pois serÃ¡ chamada de Application)
- Mantenha `SetIdForTesting` como `internal`

```csharp
using FacShopAPI.Produtos.Domain.Common;

namespace FacShopAPI.Produtos.Domain;

public class Produto
{
    public static readonly decimal PrecoMinimo = 0.01m;
    public static readonly int EstoqueMaximo = 99_999;

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

    // public (nÃ£o mais internal): chamado por Application e Infrastructure
    public void AjustarEstoque(int quantidade)
    {
        if (quantidade < 0) throw new InvalidOperationException("Estoque nÃ£o pode ser negativo.");
        if (quantidade > EstoqueMaximo) throw new InvalidOperationException($"Estoque nÃ£o pode exceder {EstoqueMaximo} unidades.");
        Estoque = quantidade;
        DataAtualizacao = DateTime.UtcNow;
    }

    // MantÃ©m internal: apenas para testes
    internal void SetIdForTesting(int id) => Id = id;
}
```

**Step 4: Adicionar InternalsVisibleTo ao Produtos.Domain.csproj**

Adicione ao `Produtos.Domain.csproj` para que os testes acessem `SetIdForTesting`:

```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
    <_Parameter1>FacShopAPI.Tests</_Parameter1>
  </AssemblyAttribute>
  <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
    <_Parameter1>Pedidos.Tests</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```

**Step 5: Compilar apenas o Domain**

```bash
dotnet build src/Produtos/Produtos.Domain/Produtos.Domain.csproj
```

Esperado: Build succeeded, 0 erros.

---

## Task 3: Criar Produtos.Application â€” DTOs, interfaces, repositÃ³rio, serviÃ§o, validadores e mapeamento

**Files:**

- Create: `src/Produtos/Produtos.Application/DTOs/ProdutoDTO.cs`
- Create: `src/Produtos/Produtos.Application/Interfaces/IProdutoContext.cs`
- Create: `src/Produtos/Produtos.Application/Repositories/IProdutoRepository.cs`
- Create: `src/Produtos/Produtos.Application/Services/IProdutoService.cs`
- Create: `src/Produtos/Produtos.Application/Services/ProdutoService.cs`
- Create: `src/Produtos/Produtos.Application/Validators/ProdutoValidator.cs`
- Create: `src/Produtos/Produtos.Application/Mappings/ProdutoMappingProfile.cs`

**Step 1: Criar DTOs**

Crie `src/Produtos/Produtos.Application/DTOs/ProdutoDTO.cs`. Copie o conteÃºdo de `src/Produtos/DTOs/ProdutoDTO.cs` e ajuste o namespace:

```csharp
namespace FacShopAPI.Produtos.Application.DTOs;

// (manter todos os tipos exatamente como antes â€” CriarProdutoRequest, AtualizarProdutoRequest,
// ProdutoResponse, PaginatedResponse<T>, PaginationInfo, ErrorResponse, AuthResponse, LoginRequest)
// Apenas o namespace muda: de FacShopAPI.Produtos.DTOs para FacShopAPI.Produtos.Application.DTOs
```

**Step 2: Criar IProdutoContext**

Crie `src/Produtos/Produtos.Application/Interfaces/IProdutoContext.cs`:

```csharp
using FacShopAPI.Produtos.Domain;

namespace FacShopAPI.Produtos.Application.Interfaces;

/// <summary>
/// AbstraÃ§Ã£o do contexto de banco para o feature de Produtos.
/// Implementada por AppDbContext no projeto principal via DI.
/// Usando IQueryable<T> (System.Linq) para evitar dependÃªncia direta do EF Core em Application.
/// </summary>
public interface IProdutoContext
{
    IQueryable<Produto> Produtos { get; }
    void AddProduto(Produto produto);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**Step 3: Criar IProdutoRepository**

Crie `src/Produtos/Produtos.Application/Repositories/IProdutoRepository.cs`:

```csharp
using FacShopAPI.Produtos.Domain;

namespace FacShopAPI.Produtos.Application.Repositories;

public interface IProdutoRepository
{
    Task<(IReadOnlyList<Produto> Items, int Total)> ListarAsync(
        int page, int pageSize, string? categoria = null, string? search = null);
    Task<Produto?> ObterPorIdAsync(int id);
    Task<Produto> AdicionarAsync(Produto produto);
    Task AtualizarAsync(Produto produto);
    Task<bool> DeletarAsync(int id);
}
```

**Step 4: Criar IProdutoService**

Crie `src/Produtos/Produtos.Application/Services/IProdutoService.cs`:

```csharp
using FacShopAPI.Produtos.Application.DTOs;

namespace FacShopAPI.Produtos.Application.Services;

public interface IProdutoService
{
    Task<PaginatedResponse<ProdutoResponse>> ListarProdutosAsync(int page, int pageSize, string? categoria = null, string? search = null);
    Task<ProdutoResponse?> ObterProdutoAsync(int id);
    Task<ProdutoResponse> CriarProdutoAsync(CriarProdutoRequest request);
    Task<ProdutoResponse?> AtualizarProdutoAsync(int id, AtualizarProdutoRequest request);
    Task<ProdutoResponse?> AtualizarCompletoProdutoAsync(int id, CriarProdutoRequest request);
    Task<bool> DeletarProdutoAsync(int id);
}
```

**Step 5: Criar ProdutoService**

Crie `src/Produtos/Produtos.Application/Services/ProdutoService.cs`. A implementaÃ§Ã£o usa `IProdutoRepository` em vez de `AppDbContext` diretamente:

```csharp
using AutoMapper;
using Microsoft.Extensions.Logging;
using FacShopAPI.Produtos.Application.DTOs;
using FacShopAPI.Produtos.Application.Repositories;
using FacShopAPI.Produtos.Domain;

namespace FacShopAPI.Produtos.Application.Services;

public class ProdutoService : IProdutoService
{
    private readonly IProdutoRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<ProdutoService> _logger;

    public ProdutoService(IProdutoRepository repository, IMapper mapper, ILogger<ProdutoService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ProdutoResponse>> ListarProdutosAsync(
        int page, int pageSize, string? categoria = null, string? search = null)
    {
        _logger.LogInformation("Listando produtos - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (produtos, total) = await _repository.ListarAsync(page, pageSize, categoria, search);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return new PaginatedResponse<ProdutoResponse>
        {
            Data = _mapper.Map<List<ProdutoResponse>>(produtos),
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = totalPages
            }
        };
    }

    public async Task<ProdutoResponse?> ObterProdutoAsync(int id)
    {
        _logger.LogInformation("Obtendo produto com ID: {ProductId}", id);
        var produto = await _repository.ObterPorIdAsync(id);
        if (produto is null)
        {
            _logger.LogWarning("Produto com ID {ProductId} nÃ£o encontrado", id);
            return null;
        }
        return _mapper.Map<ProdutoResponse>(produto);
    }

    public async Task<ProdutoResponse> CriarProdutoAsync(CriarProdutoRequest request)
    {
        _logger.LogInformation("Criando novo produto: {Nome}", request.Nome);

        var resultado = Produto.Criar(
            request.Nome, request.Descricao, request.Preco,
            request.Categoria, request.Estoque, request.ContatoEmail);

        if (!resultado.IsSuccess)
            throw new InvalidOperationException(resultado.Error);

        var produto = await _repository.AdicionarAsync(resultado.Value!);
        _logger.LogInformation("Produto criado com sucesso. ID: {ProductId}", produto.Id);
        return _mapper.Map<ProdutoResponse>(produto);
    }

    public async Task<ProdutoResponse?> AtualizarProdutoAsync(int id, AtualizarProdutoRequest request)
    {
        _logger.LogInformation("Atualizando produto com ID: {ProductId}", id);

        var produto = await _repository.ObterPorIdAsync(id);
        if (produto is null)
        {
            _logger.LogWarning("Produto com ID {ProductId} nÃ£o encontrado", id);
            return null;
        }

        if (request.Preco.HasValue)
        {
            var r = produto.AtualizarPreco(request.Preco.Value);
            if (!r.IsSuccess) throw new InvalidOperationException(r.Error);
        }
        if (request.Estoque.HasValue)
            produto.AjustarEstoque(request.Estoque.Value);

        produto.AtualizarDados(request.Nome, request.Descricao, request.Categoria, request.ContatoEmail);
        await _repository.AtualizarAsync(produto);

        _logger.LogInformation("Produto {ProductId} atualizado com sucesso", id);
        return _mapper.Map<ProdutoResponse>(produto);
    }

    public async Task<ProdutoResponse?> AtualizarCompletoProdutoAsync(int id, CriarProdutoRequest request)
    {
        _logger.LogInformation("Atualizando completamente produto com ID: {ProductId}", id);

        var produto = await _repository.ObterPorIdAsync(id);
        if (produto is null)
        {
            _logger.LogWarning("Produto com ID {ProductId} nÃ£o encontrado", id);
            return null;
        }

        if (request.Preco != produto.Preco)
        {
            var r = produto.AtualizarPreco(request.Preco);
            if (!r.IsSuccess) throw new InvalidOperationException(r.Error);
        }
        produto.AjustarEstoque(request.Estoque);
        produto.AtualizarDados(request.Nome, request.Descricao, request.Categoria, request.ContatoEmail);
        await _repository.AtualizarAsync(produto);

        _logger.LogInformation("Produto {ProductId} atualizado completamente", id);
        return _mapper.Map<ProdutoResponse>(produto);
    }

    public async Task<bool> DeletarProdutoAsync(int id)
    {
        _logger.LogInformation("Deletando produto com ID: {ProductId}", id);
        var deletado = await _repository.DeletarAsync(id);
        if (!deletado)
            _logger.LogWarning("Produto {ProductId} nÃ£o encontrado para deleÃ§Ã£o", id);
        return deletado;
    }
}
```

**Step 6: Criar validadores**

Crie `src/Produtos/Produtos.Application/Validators/ProdutoValidator.cs`. Copie de `src/Produtos/Validators/ProdutoValidator.cs` e ajuste:

```csharp
using FluentValidation;
using FacShopAPI.Produtos.Application.DTOs;

namespace FacShopAPI.Produtos.Application.Validators;

// (copiar o conteÃºdo exato de ProdutoValidator.cs, ajustando namespaces:)
// - using FacShopAPI.Produtos.DTOs â†’ using FacShopAPI.Produtos.Application.DTOs
// - namespace FacShopAPI.Produtos.Validators â†’ FacShopAPI.Produtos.Application.Validators
// Manter: CriarProdutoValidator, AtualizarProdutoValidator, LoginValidator
```

**Step 7: Criar ProdutoMappingProfile**

Crie `src/Produtos/Produtos.Application/Mappings/ProdutoMappingProfile.cs`:

```csharp
using AutoMapper;
using FacShopAPI.Produtos.Application.DTOs;
using FacShopAPI.Produtos.Domain;

namespace FacShopAPI.Produtos.Application.Mappings;

public class ProdutoMappingProfile : Profile
{
    public ProdutoMappingProfile()
    {
        CreateMap<Produto, ProdutoResponse>();
    }
}
```

**Step 8: Compilar Application**

```bash
dotnet build src/Produtos/Produtos.Application/Produtos.Application.csproj
```

Esperado: Build succeeded, 0 erros.

---

## Task 4: Criar Produtos.Infrastructure â€” EfProdutoRepository e DbSeeder

**Files:**

- Create: `src/Produtos/Produtos.Infrastructure/Repositories/EfProdutoRepository.cs`
- Create: `src/Produtos/Produtos.Infrastructure/Data/DbSeeder.cs`

> **Nota:** `AppDbContext` permanece em `src/Shared/Data/AppDbContext.cs` no projeto principal. O repositÃ³rio recebe `IProdutoContext` (interface definida em Application) via injeÃ§Ã£o de dependÃªncia. O `EfProdutoRepository` usa `ToListAsync()` e `CountAsync()` do EF Core â€” por isso `Produtos.Infrastructure` jÃ¡ referencia `Microsoft.EntityFrameworkCore`.

**Step 1: Escrever o teste de integraÃ§Ã£o do repositÃ³rio**

O repositÃ³rio serÃ¡ testado via os testes de integraÃ§Ã£o existentes (`ProdutoEndpointsTests.cs`). Confirme que estÃ£o passando agora antes de continuar:

```bash
dotnet test FacShopAPI.Tests/FacShopAPI.Tests.csproj --filter "FullyQualifiedName~ProdutoEndpointsTests" --no-build
```

Esperado: 23 passed.

**Step 2: Criar EfProdutoRepository**

Crie `src/Produtos/Produtos.Infrastructure/Repositories/EfProdutoRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FacShopAPI.Produtos.Application.Interfaces;
using FacShopAPI.Produtos.Application.Repositories;
using FacShopAPI.Produtos.Domain;

namespace FacShopAPI.Produtos.Infrastructure.Repositories;

public class EfProdutoRepository : IProdutoRepository
{
    private readonly IProdutoContext _context;
    private readonly ILogger<EfProdutoRepository> _logger;

    public EfProdutoRepository(IProdutoContext context, ILogger<EfProdutoRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<Produto> Items, int Total)> ListarAsync(
        int page, int pageSize, string? categoria = null, string? search = null)
    {
        var query = _context.Produtos.Where(p => p.Ativo);

        if (!string.IsNullOrEmpty(categoria))
            query = query.Where(p => p.Categoria == categoria);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Nome.Contains(search) || p.Descricao.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.DataCriacao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Produto?> ObterPorIdAsync(int id)
    {
        return await _context.Produtos
            .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);
    }

    public async Task<Produto> AdicionarAsync(Produto produto)
    {
        _context.AddProduto(produto);
        await _context.SaveChangesAsync();
        return produto;
    }

    public async Task AtualizarAsync(Produto produto)
    {
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeletarAsync(int id)
    {
        var produto = await _context.Produtos
            .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);

        if (produto is null)
        {
            _logger.LogWarning("Produto {Id} nÃ£o encontrado", id);
            return false;
        }

        var result = produto.Desativar();
        if (!result.IsSuccess) return false;

        await _context.SaveChangesAsync();
        return true;
    }
}
```

**Step 3: Criar DbSeeder em Infrastructure**

Crie `src/Produtos/Produtos.Infrastructure/Data/DbSeeder.cs`. Copie de `src/Shared/Data/DbSeeder.cs` e ajuste os namespaces:

```csharp
using FacShopAPI.Produtos.Application.Interfaces;
using FacShopAPI.Produtos.Domain;

namespace FacShopAPI.Produtos.Infrastructure.Data;

public static class DbSeeder
{
    public static void Seed(IProdutoContext context)
    {
        if (context.Produtos.Any()) return;

        var produtos = new List<Produto>
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

        foreach (var p in produtos)
            context.AddProduto(p);

        context.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
```

**Step 4: Compilar Infrastructure**

```bash
dotnet build src/Produtos/Produtos.Infrastructure/Produtos.Infrastructure.csproj
```

Esperado: Build succeeded, 0 erros.

---

## Task 5: Criar Produtos.API â€” Endpoints e extensÃ£o de DI

**Files:**

- Create: `src/Produtos/Produtos.API/Endpoints/ProdutoEndpoints.cs`
- Create: `src/Produtos/Produtos.API/Endpoints/AuthEndpoints.cs`
- Create: `src/Produtos/Produtos.API/Extensions/ProdutosServiceExtensions.cs`

**Step 1: Criar ProdutoEndpoints.cs**

Copie `src/Produtos/Endpoints/ProdutoEndpoints.cs` para `src/Produtos/Produtos.API/Endpoints/ProdutoEndpoints.cs`. Ajuste os `using`:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http;
using FacShopAPI.Produtos.Application.DTOs;
using FacShopAPI.Produtos.Application.Services;

namespace FacShopAPI.Produtos.API.Endpoints;

// (manter todo o cÃ³digo do endpoint exatamente como estÃ¡,
//  sÃ³ alterar o namespace e os usings acima)
```

**Step 2: Criar AuthEndpoints.cs**

Copie `src/Produtos/Endpoints/AuthEndpoints.cs` para `src/Produtos/Produtos.API/Endpoints/AuthEndpoints.cs`. Ajuste:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using FacShopAPI.Produtos.Application.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FacShopAPI.Produtos.API.Endpoints;

// (manter o cÃ³digo exato, apenas namespace muda)
```

**Step 3: Criar ProdutosServiceExtensions.cs**

Crie `src/Produtos/Produtos.API/Extensions/ProdutosServiceExtensions.cs`. Esta classe centraliza o registro de todos os serviÃ§os do feature Produtos:

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using FacShopAPI.Produtos.Application.Repositories;
using FacShopAPI.Produtos.Application.Services;
using FacShopAPI.Produtos.Application.Validators;
using FacShopAPI.Produtos.Infrastructure.Repositories;

namespace FacShopAPI.Produtos.API.Extensions;

public static class ProdutosServiceExtensions
{
    /// <summary>
    /// Registra todos os serviÃ§os do feature Produtos no container de DI.
    /// Chamada em Program.cs: builder.Services.AddProdutos();
    /// </summary>
    public static IServiceCollection AddProdutos(this IServiceCollection services)
    {
        // Application services
        services.AddScoped<IProdutoService, ProdutoService>();

        // Repository (Infrastructure)
        services.AddScoped<IProdutoRepository, EfProdutoRepository>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<CriarProdutoValidator>();

        return services;
    }
}
```

**Step 4: Compilar Produtos.API**

```bash
dotnet build src/Produtos/Produtos.API/Produtos.API.csproj
```

Esperado: Build succeeded, 0 erros.

---

## Task 6: Atualizar AppDbContext para implementar IProdutoContext

**Files:**

- Modify: `src/Shared/Data/AppDbContext.cs`

O `AppDbContext` precisa:

1. Adicionar `using FacShopAPI.Produtos.Application.Interfaces;`
2. Implementar `IProdutoContext` na declaraÃ§Ã£o da classe
3. Adicionar o mÃ©todo `AddProduto`
4. Ajustar o `using` de `Produto` (que mudou de namespace)

**Step 1: Atualizar AppDbContext**

```csharp
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Pedidos.Domain;
using FacShopAPI.Produtos.Application.Interfaces;  // novo
using FacShopAPI.Produtos.Domain;                  // namespace novo (era Produtos.Models)

namespace FacShopAPI.Shared.Data;

public class AppDbContext : DbContext, IProdutoContext  // adicionar IProdutoContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();

    // IProdutoContext: IQueryable<Produto> Produtos jÃ¡ satisfeito pelo DbSet acima
    // IProdutoContext: SaveChangesAsync jÃ¡ satisfeito por DbContext

    public void AddProduto(Produto produto) => this.Add(produto);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // (manter OnModelCreating exatamente como estÃ¡)
    }
}
```

**Step 2: Verificar que o projeto principal ainda compila**

```bash
dotnet build FacShopAPI.csproj
```

Esperado: warning sobre using duplicado (jÃ¡ existente), mas 0 erros.

---

## Task 7: Atualizar FacShopAPI.csproj e Program.cs

**Files:**

- Modify: `FacShopAPI.csproj`
- Modify: `Program.cs`

**Step 1: Atualizar FacShopAPI.csproj**

Adicione as referÃªncias aos sub-projetos no `FacShopAPI.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="src/Produtos/Produtos.API/Produtos.API.csproj" />
  <ProjectReference Include="src/Produtos/Produtos.Infrastructure/Produtos.Infrastructure.csproj" />
</ItemGroup>
```

Remova os pacotes NuGet que foram movidos para os sub-projetos (AutoMapper, FluentValidation permanecerÃ£o se ainda usados por Pedidos; inspecionar antes de remover):

- `AutoMapper` â†’ mover para Application (jÃ¡ estÃ¡)
- `FluentValidation` e `FluentValidation.DependencyInjectionExtensions` â†’ manter no projeto principal se Pedidos usa

**Step 2: Atualizar Program.cs**

Principais mudanÃ§as:

1. Trocar `using` de namespaces antigos pelos novos
2. Trocar `typeof(MappingProfile)` por `typeof(ProdutoMappingProfile)`
3. Substituir registro manual de `IProdutoService` e validators por `builder.Services.AddProdutos()`
4. Registrar `IProdutoContext` â†’ `AppDbContext` para injeÃ§Ã£o de dependÃªncia do repositÃ³rio
5. Atualizar `MapProdutoEndpoints()` e `MapAuthEndpoints()` (namespace mudou mas nomes dos mÃ©todos sÃ£o iguais)
6. Atualizar `DbSeeder.Seed()` para nova assinatura

```csharp
// Remover usings antigos:
// using FacShopAPI.Produtos.DTOs;
// using FacShopAPI.Produtos.Endpoints;
// using FacShopAPI.Produtos.Services;
// using FacShopAPI.Produtos.Validators;

// Adicionar novos usings:
using FacShopAPI.Produtos.API.Endpoints;
using FacShopAPI.Produtos.API.Extensions;
using FacShopAPI.Produtos.Application.Interfaces;
using FacShopAPI.Produtos.Application.Mappings;
using FacShopAPI.Produtos.Infrastructure.Data;

// Trocar:
// builder.Services.AddScoped<IProdutoService, ProdutoService>();
// builder.Services.AddValidatorsFromAssemblyContaining<CriarProdutoValidator>();
// Por:
builder.Services.AddProdutos();

// Adicionar para conectar AppDbContext â†’ IProdutoContext:
builder.Services.AddScoped<IProdutoContext>(sp => sp.GetRequiredService<AppDbContext>());

// Trocar:
// builder.Services.AddAutoMapper(typeof(MappingProfile));
// Por:
builder.Services.AddAutoMapper(typeof(ProdutoMappingProfile));

// No seed:
// DbSeeder.Seed(dbContext); â†’ DbSeeder.Seed(dbContext); (mesmo chamada, mas agora aceita IProdutoContext)
// Como AppDbContext : IProdutoContext, a chamada funciona com cast implÃ­cito
```

**Step 3: Compilar o projeto principal**

```bash
dotnet build FacShopAPI.csproj
```

Esperado: 0 erros. Pode haver warnings sobre `using` duplicados ou obsoletos â€” corrija-os.

---

## Task 8: Atualizar soluÃ§Ã£o e projetos de teste

**Files:**

- Modify: `FacShopAPI.slnx`
- Modify: `FacShopAPI.Tests/FacShopAPI.Tests.csproj`
- Modify: `FacShopAPI.Tests/Builders/ProdutoBuilder.cs`
- Modify: `FacShopAPI.Tests/Unit/Domain/ProdutoTests.cs`
- Modify: `FacShopAPI.Tests/Services/ProdutoServiceTests.cs`

**Step 1: Adicionar sub-projetos ao FacShopAPI.slnx**

```xml
<Solution>
  <Project Path="FacShopAPI.csproj" />
  <Project Path="src/Produtos/Produtos.Domain/Produtos.Domain.csproj" />
  <Project Path="src/Produtos/Produtos.Application/Produtos.Application.csproj" />
  <Project Path="src/Produtos/Produtos.Infrastructure/Produtos.Infrastructure.csproj" />
  <Project Path="src/Produtos/Produtos.API/Produtos.API.csproj" />
  <Project Path="FacShopAPI.Tests/FacShopAPI.Tests.csproj" />
  <Project Path="Pedidos.Tests/Pedidos.Tests.csproj" />
</Solution>
```

**Step 2: Atualizar FacShopAPI.Tests.csproj**

Adicionar referÃªncias diretas para acessar tipos internos dos sub-projetos:

```xml
<ItemGroup>
  <ProjectReference Include="../FacShopAPI.csproj" />
  <ProjectReference Include="../src/Produtos/Produtos.Domain/Produtos.Domain.csproj" />
  <ProjectReference Include="../src/Produtos/Produtos.Application/Produtos.Application.csproj" />
  <ProjectReference Include="../src/Produtos/Produtos.Infrastructure/Produtos.Infrastructure.csproj" />
</ItemGroup>
```

**Step 3: Atualizar ProdutoBuilder.cs**

Altere o `using`:

```csharp
// Antes:
using FacShopAPI.Produtos.Models;
// Depois:
using FacShopAPI.Produtos.Domain;
```

**Step 4: Atualizar ProdutoTests.cs**

Altere o `using`:

```csharp
// Antes:
using FacShopAPI.Produtos.Models;
// Depois:
using FacShopAPI.Produtos.Domain;
using FacShopAPI.Produtos.Domain.Common;
```

Verificar se os asserts que usam `Result` precisam do namespace novo.

**Step 5: Atualizar ProdutoServiceTests.cs**

Os testes do serviÃ§o atualmente mockam `AppDbContext` e `IMapper`. Com a nova arquitetura, `ProdutoService` recebe `IProdutoRepository` e `IMapper`. Reescreva os mocks:

```csharp
// Antes: var mockContext = new Mock<AppDbContext>(...);
// Depois: var mockRepository = new Mock<IProdutoRepository>();

// Antes: var service = new ProdutoService(mockContext.Object, mockMapper.Object, logger);
// Depois: var service = new ProdutoService(mockRepository.Object, mockMapper.Object, logger);
```

Adicione usings:

```csharp
using FacShopAPI.Produtos.Application.Repositories;
using FacShopAPI.Produtos.Application.Services;
using FacShopAPI.Produtos.Application.DTOs;
using FacShopAPI.Produtos.Domain;
```

**Step 6: Atualizar ProdutoValidatorTests.cs e ProdutoEndpointsTests.cs**

Atualizar os `using` de DTOs:

```csharp
// Antes: using FacShopAPI.Produtos.DTOs;
// Depois: using FacShopAPI.Produtos.Application.DTOs;
```

---

## Task 9: Build e validaÃ§Ã£o completa

**Step 1: Build da soluÃ§Ã£o completa**

```bash
dotnet build FacShopAPI.slnx
```

Esperado: 0 erros em todos os projetos.

**Step 2: Rodar todos os testes**

```bash
dotnet test FacShopAPI.Tests/FacShopAPI.Tests.csproj
```

Esperado: 112 passed, 0 failed.

```bash
dotnet test Pedidos.Tests/Pedidos.Tests.csproj
```

Esperado: todos os testes passando.

**Step 3: Verificar a aplicaÃ§Ã£o sobe corretamente**

```bash
dotnet run --project FacShopAPI.csproj
```

Esperado: `Now listening on: http://localhost:5000`. Abra http://localhost:5000 para verificar o Swagger UI.

---

## Task 10: Remover cÃ³digo antigo e commit

**Files:**

- Delete: `src/Produtos/DTOs/` (conteÃºdo migrado para Produtos.Application)
- Delete: `src/Produtos/Models/` (migrado para Produtos.Domain)
- Delete: `src/Produtos/Services/` (migrado para Produtos.Application)
- Delete: `src/Produtos/Validators/` (migrado para Produtos.Application)
- Delete: `src/Produtos/Endpoints/` (migrado para Produtos.API)
- Delete: `src/Shared/Data/DbSeeder.cs` (migrado para Produtos.Infrastructure)
- Delete: `src/Shared/Common/MappingProfile.cs` (substituÃ­do por ProdutoMappingProfile)
- Modify: `src/InternalsVisibleTo.cs` â†’ pode ser removido se os InternalsVisibleTo estiverem nos .csproj

**Step 1: Remover diretÃ³rios antigos**

```bash
rm -rf src/Produtos/DTOs
rm -rf src/Produtos/Models
rm -rf src/Produtos/Services
rm -rf src/Produtos/Validators
rm -rf src/Produtos/Endpoints
rm src/Shared/Data/DbSeeder.cs
rm src/Shared/Common/MappingProfile.cs
```

**Step 2: Rodar todos os testes novamente para confirmar**

```bash
dotnet test FacShopAPI.slnx
```

Esperado: todos os testes passando.

**Step 3: Commit**

```bash
git add .
git commit -m "refactor: migrar Produtos para clean architecture com 4 sub-projetos

- Produtos.Domain: entidade Produto + Result (sem dependÃªncias externas)
- Produtos.Application: DTOs, IProdutoService, ProdutoService, IProdutoRepository,
  validadores, ProdutoMappingProfile, IProdutoContext
- Produtos.Infrastructure: EfProdutoRepository, DbSeeder
- Produtos.API: endpoints, AuthEndpoints, ProdutosServiceExtensions
- AppDbContext implementa IProdutoContext (inversÃ£o de dependÃªncia)
- Testes atualizados para novos namespaces"
```

---

## Diagrama de DependÃªncias

```
FacShopAPI.csproj (host)
â”œâ”€â”€ src/Pedidos/ â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ (inalterado)
â”œâ”€â”€ src/Shared/Data/AppDbContext.cs â”€ implements IProdutoContext
â”‚
â”œâ”€â”€ Produtos.API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€ (ASP.NET endpoints)
â”‚   â”œâ”€â”€ â†’ Produtos.Application
â”‚   â””â”€â”€ â†’ Produtos.Infrastructure (para DI)
â”‚
â”œâ”€â”€ Produtos.Infrastructure â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  (EF Core repos)
â”‚   â”œâ”€â”€ â†’ Produtos.Application (IProdutoRepository, IProdutoContext)
â”‚   â””â”€â”€ â†’ Produtos.Domain
â”‚
â”œâ”€â”€ Produtos.Application â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  (use cases, DTOs)
â”‚   â””â”€â”€ â†’ Produtos.Domain
â”‚
â””â”€â”€ Produtos.Domain â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  (entidades puras, sem deps)
```

**Fluxo de uma requisiÃ§Ã£o `POST /api/v1/produtos`:**

```
HTTP â†’ ProdutoEndpoints.cs (Produtos.API)
     â†’ CriarProdutoValidator (Produtos.Application)
     â†’ IProdutoService â†’ ProdutoService (Produtos.Application)
     â†’ IProdutoRepository â†’ EfProdutoRepository (Produtos.Infrastructure)
     â†’ IProdutoContext â†’ AppDbContext (FacShopAPI.csproj)
     â†’ SQLite / InMemory DB
```
