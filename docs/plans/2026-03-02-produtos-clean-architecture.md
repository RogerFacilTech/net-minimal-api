# Produtos Clean Architecture Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrar o feature Produtos para arquitetura limpa em 4 sub-projetos (`Produtos.Domain`, `Produtos.Application`, `Produtos.Infrastructure`, `Produtos.API`) dentro de `src/Produtos/`, mantendo o projeto `ProdutosAPI.csproj` como host e sem quebrar os testes existentes.

**Architecture:** Domain sem dependências externas; Application depende de Domain e define abstrações (interfaces); Infrastructure implementa repositório com EF Core via interface definida em Application; API contém endpoints e wiring de DI. O `AppDbContext` permanece no projeto principal (cross-cutting entre Produtos e Pedidos) e implementa a interface `IProdutoContext` definida em Application. O fluxo de dependências é: `API → Application ← Infrastructure` e `API → Infrastructure` (para DI).

**Tech Stack:** .NET 10, xUnit, FluentAssertions, EF Core 10, AutoMapper 12, FluentValidation 11, Microsoft.AspNetCore.App (FrameworkReference).

---

## Mapa de Mudanças de Namespace

| Antes                                      | Depois                                                             |
| ------------------------------------------ | ------------------------------------------------------------------ |
| `ProdutosAPI.Produtos.Models.Produto`      | `ProdutosAPI.Produtos.Domain.Produto`                              |
| `ProdutosAPI.Shared.Common.MappingProfile` | `ProdutosAPI.Produtos.Application.Mappings.ProdutoMappingProfile`  |
| `ProdutosAPI.Produtos.DTOs.*`              | `ProdutosAPI.Produtos.Application.DTOs.*` (mesmos nomes de classe) |
| `ProdutosAPI.Produtos.Services.*`          | `ProdutosAPI.Produtos.Application.Services.*` (mesmos nomes)       |
| `ProdutosAPI.Produtos.Validators.*`        | `ProdutosAPI.Produtos.Application.Validators.*`                    |
| `ProdutosAPI.Produtos.Endpoints.*`         | `ProdutosAPI.Produtos.API.Endpoints.*`                             |
| `ProdutosAPI.Shared.Data.DbSeeder`         | `ProdutosAPI.Produtos.Infrastructure.Data.DbSeeder`                |

---

## Estrutura de Destino

```
src/Produtos/
├── Produtos.Domain/
│   ├── Produtos.Domain.csproj
│   ├── Produto.cs
│   └── Common/
│       └── Result.cs
├── Produtos.Application/
│   ├── Produtos.Application.csproj
│   ├── DTOs/
│   │   └── ProdutoDTO.cs
│   ├── Interfaces/
│   │   └── IProdutoContext.cs
│   ├── Repositories/
│   │   └── IProdutoRepository.cs
│   ├── Services/
│   │   ├── IProdutoService.cs
│   │   └── ProdutoService.cs
│   ├── Validators/
│   │   └── ProdutoValidator.cs
│   └── Mappings/
│       └── ProdutoMappingProfile.cs
├── Produtos.Infrastructure/
│   ├── Produtos.Infrastructure.csproj
│   ├── Repositories/
│   │   └── EfProdutoRepository.cs
│   └── Data/
│       └── DbSeeder.cs
└── Produtos.API/
    ├── Produtos.API.csproj
    ├── Endpoints/
    │   ├── ProdutoEndpoints.cs
    │   └── AuthEndpoints.cs
    └── Extensions/
        └── ProdutosServiceExtensions.cs
```

---

## Task 1: Criar estrutura de diretórios e arquivos de projeto (.csproj)

**Files:**

- Create: `src/Produtos/Produtos.Domain/Produtos.Domain.csproj`
- Create: `src/Produtos/Produtos.Application/Produtos.Application.csproj`
- Create: `src/Produtos/Produtos.Infrastructure/Produtos.Infrastructure.csproj`
- Create: `src/Produtos/Produtos.API/Produtos.API.csproj`

**Step 1: Criar diretórios**

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
    <RootNamespace>ProdutosAPI.Produtos.Domain</RootNamespace>
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
    <RootNamespace>ProdutosAPI.Produtos.Application</RootNamespace>
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
    <RootNamespace>ProdutosAPI.Produtos.Infrastructure</RootNamespace>
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
    <RootNamespace>ProdutosAPI.Produtos.API</RootNamespace>
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

Esperado: cada diretório deve conter o `.csproj` e subdiretórios.

---

## Task 2: Criar Produtos.Domain — Produto.cs e Result.cs

**Files:**

- Create: `src/Produtos/Produtos.Domain/Common/Result.cs`
- Create: `src/Produtos/Produtos.Domain/Produto.cs`
- Source: `src/Produtos/Models/Produto.cs` (referência — não apagar ainda)

**Step 1: Escrever o teste que verifica compilação da entidade**

O teste já existe em `ProdutosAPI.Tests/Unit/Domain/ProdutoTests.cs`. Depois de migrar o namespace, ele deve continuar passando. Rodar os testes agora para ver o estado atual:

```bash
dotnet test ProdutosAPI.Tests/ProdutosAPI.Tests.csproj --filter "FullyQualifiedName~ProdutoTests" --no-build
```

Esperado: PASS (estado atual, antes da migração).

**Step 2: Criar Result.cs em Domain**

Crie `src/Produtos/Produtos.Domain/Common/Result.cs`:

```csharp
namespace ProdutosAPI.Produtos.Domain.Common;

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

Crie `src/Produtos/Produtos.Domain/Produto.cs`. Copie o conteúdo de `src/Produtos/Models/Produto.cs` e ajuste:

- Namespace: `ProdutosAPI.Produtos.Domain`
- Using: troque `using ProdutosAPI.Shared.Common;` por `using ProdutosAPI.Produtos.Domain.Common;`
- Torne `AjustarEstoque` `public` (não mais `internal`, pois será chamada de Application)
- Mantenha `SetIdForTesting` como `internal`

```csharp
using ProdutosAPI.Produtos.Domain.Common;

namespace ProdutosAPI.Produtos.Domain;

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
            return Result<Produto>.Fail("Preço deve ser maior que zero.");
        if (estoque < 0)
            return Result<Produto>.Fail("Estoque não pode ser negativo.");
        if (string.IsNullOrWhiteSpace(email))
            return Result<Produto>.Fail("Email de contato é obrigatório.");

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
            return Result.Fail("Preço deve ser maior que zero.");
        if (novoPreco == Preco)
            return Result.Fail("Novo preço é igual ao preço atual.");
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
            return Result.Fail("Quantidade de reposição deve ser positiva.");
        if (Estoque + quantidade > EstoqueMaximo)
            return Result.Fail($"Estoque não pode exceder {EstoqueMaximo} unidades.");
        Estoque += quantidade;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result Desativar()
    {
        if (!Ativo)
            return Result.Fail("Produto já está inativo.");
        Ativo = false;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public bool TemEstoqueDisponivel(int qtd) => Ativo && Estoque >= qtd;

    // public (não mais internal): chamado por Application e Infrastructure
    public void AjustarEstoque(int quantidade)
    {
        if (quantidade < 0) throw new InvalidOperationException("Estoque não pode ser negativo.");
        if (quantidade > EstoqueMaximo) throw new InvalidOperationException($"Estoque não pode exceder {EstoqueMaximo} unidades.");
        Estoque = quantidade;
        DataAtualizacao = DateTime.UtcNow;
    }

    // Mantém internal: apenas para testes
    internal void SetIdForTesting(int id) => Id = id;
}
```

**Step 4: Adicionar InternalsVisibleTo ao Produtos.Domain.csproj**

Adicione ao `Produtos.Domain.csproj` para que os testes acessem `SetIdForTesting`:

```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
    <_Parameter1>ProdutosAPI.Tests</_Parameter1>
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

## Task 3: Criar Produtos.Application — DTOs, interfaces, repositório, serviço, validadores e mapeamento

**Files:**

- Create: `src/Produtos/Produtos.Application/DTOs/ProdutoDTO.cs`
- Create: `src/Produtos/Produtos.Application/Interfaces/IProdutoContext.cs`
- Create: `src/Produtos/Produtos.Application/Repositories/IProdutoRepository.cs`
- Create: `src/Produtos/Produtos.Application/Services/IProdutoService.cs`
- Create: `src/Produtos/Produtos.Application/Services/ProdutoService.cs`
- Create: `src/Produtos/Produtos.Application/Validators/ProdutoValidator.cs`
- Create: `src/Produtos/Produtos.Application/Mappings/ProdutoMappingProfile.cs`

**Step 1: Criar DTOs**

Crie `src/Produtos/Produtos.Application/DTOs/ProdutoDTO.cs`. Copie o conteúdo de `src/Produtos/DTOs/ProdutoDTO.cs` e ajuste o namespace:

```csharp
namespace ProdutosAPI.Produtos.Application.DTOs;

// (manter todos os tipos exatamente como antes — CriarProdutoRequest, AtualizarProdutoRequest,
// ProdutoResponse, PaginatedResponse<T>, PaginationInfo, ErrorResponse, AuthResponse, LoginRequest)
// Apenas o namespace muda: de ProdutosAPI.Produtos.DTOs para ProdutosAPI.Produtos.Application.DTOs
```

**Step 2: Criar IProdutoContext**

Crie `src/Produtos/Produtos.Application/Interfaces/IProdutoContext.cs`:

```csharp
using ProdutosAPI.Produtos.Domain;

namespace ProdutosAPI.Produtos.Application.Interfaces;

/// <summary>
/// Abstração do contexto de banco para o feature de Produtos.
/// Implementada por AppDbContext no projeto principal via DI.
/// Usando IQueryable<T> (System.Linq) para evitar dependência direta do EF Core em Application.
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
using ProdutosAPI.Produtos.Domain;

namespace ProdutosAPI.Produtos.Application.Repositories;

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
using ProdutosAPI.Produtos.Application.DTOs;

namespace ProdutosAPI.Produtos.Application.Services;

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

Crie `src/Produtos/Produtos.Application/Services/ProdutoService.cs`. A implementação usa `IProdutoRepository` em vez de `AppDbContext` diretamente:

```csharp
using AutoMapper;
using Microsoft.Extensions.Logging;
using ProdutosAPI.Produtos.Application.DTOs;
using ProdutosAPI.Produtos.Application.Repositories;
using ProdutosAPI.Produtos.Domain;

namespace ProdutosAPI.Produtos.Application.Services;

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
            _logger.LogWarning("Produto com ID {ProductId} não encontrado", id);
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
            _logger.LogWarning("Produto com ID {ProductId} não encontrado", id);
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
            _logger.LogWarning("Produto com ID {ProductId} não encontrado", id);
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
            _logger.LogWarning("Produto {ProductId} não encontrado para deleção", id);
        return deletado;
    }
}
```

**Step 6: Criar validadores**

Crie `src/Produtos/Produtos.Application/Validators/ProdutoValidator.cs`. Copie de `src/Produtos/Validators/ProdutoValidator.cs` e ajuste:

```csharp
using FluentValidation;
using ProdutosAPI.Produtos.Application.DTOs;

namespace ProdutosAPI.Produtos.Application.Validators;

// (copiar o conteúdo exato de ProdutoValidator.cs, ajustando namespaces:)
// - using ProdutosAPI.Produtos.DTOs → using ProdutosAPI.Produtos.Application.DTOs
// - namespace ProdutosAPI.Produtos.Validators → ProdutosAPI.Produtos.Application.Validators
// Manter: CriarProdutoValidator, AtualizarProdutoValidator, LoginValidator
```

**Step 7: Criar ProdutoMappingProfile**

Crie `src/Produtos/Produtos.Application/Mappings/ProdutoMappingProfile.cs`:

```csharp
using AutoMapper;
using ProdutosAPI.Produtos.Application.DTOs;
using ProdutosAPI.Produtos.Domain;

namespace ProdutosAPI.Produtos.Application.Mappings;

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

## Task 4: Criar Produtos.Infrastructure — EfProdutoRepository e DbSeeder

**Files:**

- Create: `src/Produtos/Produtos.Infrastructure/Repositories/EfProdutoRepository.cs`
- Create: `src/Produtos/Produtos.Infrastructure/Data/DbSeeder.cs`

> **Nota:** `AppDbContext` permanece em `src/Shared/Data/AppDbContext.cs` no projeto principal. O repositório recebe `IProdutoContext` (interface definida em Application) via injeção de dependência. O `EfProdutoRepository` usa `ToListAsync()` e `CountAsync()` do EF Core — por isso `Produtos.Infrastructure` já referencia `Microsoft.EntityFrameworkCore`.

**Step 1: Escrever o teste de integração do repositório**

O repositório será testado via os testes de integração existentes (`ProdutoEndpointsTests.cs`). Confirme que estão passando agora antes de continuar:

```bash
dotnet test ProdutosAPI.Tests/ProdutosAPI.Tests.csproj --filter "FullyQualifiedName~ProdutoEndpointsTests" --no-build
```

Esperado: 23 passed.

**Step 2: Criar EfProdutoRepository**

Crie `src/Produtos/Produtos.Infrastructure/Repositories/EfProdutoRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProdutosAPI.Produtos.Application.Interfaces;
using ProdutosAPI.Produtos.Application.Repositories;
using ProdutosAPI.Produtos.Domain;

namespace ProdutosAPI.Produtos.Infrastructure.Repositories;

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
            _logger.LogWarning("Produto {Id} não encontrado", id);
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
using ProdutosAPI.Produtos.Application.Interfaces;
using ProdutosAPI.Produtos.Domain;

namespace ProdutosAPI.Produtos.Infrastructure.Data;

public static class DbSeeder
{
    public static void Seed(IProdutoContext context)
    {
        if (context.Produtos.Any()) return;

        var produtos = new List<Produto>
        {
            Produto.Criar("Notebook Dell XPS 13", "Notebook de alta performance com processador Intel Core i7, 16GB RAM e 512GB SSD", 4500.00m, "Eletrônicos", 5, "vendas@dell.com").Value!,
            Produto.Criar("Mouse Logitech MX Master 3S", "Mouse wireless de precisão profissional com múltiplos botões e rastreamento avançado", 450.00m, "Eletrônicos", 25, "suporte@logitech.com").Value!,
            Produto.Criar("Teclado Mecânico RGB", "Teclado mecânico com iluminação RGB, switches Cherry MX e design compacto", 350.00m, "Eletrônicos", 15, "contato@keyboards.com.br").Value!,
            Produto.Criar("Clean Code", "Guia prático para escrever código limpo e manutenível. Essencial para todo desenvolvedor", 89.90m, "Livros", 30, "vendas@books.com").Value!,
            Produto.Criar("Design Patterns", "Padrões de design reutilizáveis para desenvolvimento de software. Referência obrigatória", 75.00m, "Livros", 20, "vendas@books.com").Value!,
            Produto.Criar("Camiseta técnica Azul", "Camiseta de poliéster com tecnologia anti-transpiração, disponível em vários tamanhos", 79.90m, "Roupas", 50, "vendas@clothing.com.br").Value!,
            Produto.Criar("Café Gourmet 500g", "Café gourmet especial com grãos selecionados de plantações premium da região", 45.00m, "Alimentos", 100, "vendas@coffee.com.br").Value!,
            Produto.Criar("Monitor LG UltraWide 34\"", "Monitor curvo ultrawide com resolução 3440x1440, ideal para produtividade e games", 1899.00m, "Eletrônicos", 3, "suporte@lg.com.br").Value!
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

## Task 5: Criar Produtos.API — Endpoints e extensão de DI

**Files:**

- Create: `src/Produtos/Produtos.API/Endpoints/ProdutoEndpoints.cs`
- Create: `src/Produtos/Produtos.API/Endpoints/AuthEndpoints.cs`
- Create: `src/Produtos/Produtos.API/Extensions/ProdutosServiceExtensions.cs`

**Step 1: Criar ProdutoEndpoints.cs**

Copie `src/Produtos/Endpoints/ProdutoEndpoints.cs` para `src/Produtos/Produtos.API/Endpoints/ProdutoEndpoints.cs`. Ajuste os `using`:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http;
using ProdutosAPI.Produtos.Application.DTOs;
using ProdutosAPI.Produtos.Application.Services;

namespace ProdutosAPI.Produtos.API.Endpoints;

// (manter todo o código do endpoint exatamente como está,
//  só alterar o namespace e os usings acima)
```

**Step 2: Criar AuthEndpoints.cs**

Copie `src/Produtos/Endpoints/AuthEndpoints.cs` para `src/Produtos/Produtos.API/Endpoints/AuthEndpoints.cs`. Ajuste:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using ProdutosAPI.Produtos.Application.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProdutosAPI.Produtos.API.Endpoints;

// (manter o código exato, apenas namespace muda)
```

**Step 3: Criar ProdutosServiceExtensions.cs**

Crie `src/Produtos/Produtos.API/Extensions/ProdutosServiceExtensions.cs`. Esta classe centraliza o registro de todos os serviços do feature Produtos:

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProdutosAPI.Produtos.Application.Repositories;
using ProdutosAPI.Produtos.Application.Services;
using ProdutosAPI.Produtos.Application.Validators;
using ProdutosAPI.Produtos.Infrastructure.Repositories;

namespace ProdutosAPI.Produtos.API.Extensions;

public static class ProdutosServiceExtensions
{
    /// <summary>
    /// Registra todos os serviços do feature Produtos no container de DI.
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

1. Adicionar `using ProdutosAPI.Produtos.Application.Interfaces;`
2. Implementar `IProdutoContext` na declaração da classe
3. Adicionar o método `AddProduto`
4. Ajustar o `using` de `Produto` (que mudou de namespace)

**Step 1: Atualizar AppDbContext**

```csharp
using Microsoft.EntityFrameworkCore;
using ProdutosAPI.Pedidos.Domain;
using ProdutosAPI.Produtos.Application.Interfaces;  // novo
using ProdutosAPI.Produtos.Domain;                  // namespace novo (era Produtos.Models)

namespace ProdutosAPI.Shared.Data;

public class AppDbContext : DbContext, IProdutoContext  // adicionar IProdutoContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();

    // IProdutoContext: IQueryable<Produto> Produtos já satisfeito pelo DbSet acima
    // IProdutoContext: SaveChangesAsync já satisfeito por DbContext

    public void AddProduto(Produto produto) => this.Add(produto);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // (manter OnModelCreating exatamente como está)
    }
}
```

**Step 2: Verificar que o projeto principal ainda compila**

```bash
dotnet build ProdutosAPI.csproj
```

Esperado: warning sobre using duplicado (já existente), mas 0 erros.

---

## Task 7: Atualizar ProdutosAPI.csproj e Program.cs

**Files:**

- Modify: `ProdutosAPI.csproj`
- Modify: `Program.cs`

**Step 1: Atualizar ProdutosAPI.csproj**

Adicione as referências aos sub-projetos no `ProdutosAPI.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="src/Produtos/Produtos.API/Produtos.API.csproj" />
  <ProjectReference Include="src/Produtos/Produtos.Infrastructure/Produtos.Infrastructure.csproj" />
</ItemGroup>
```

Remova os pacotes NuGet que foram movidos para os sub-projetos (AutoMapper, FluentValidation permanecerão se ainda usados por Pedidos; inspecionar antes de remover):

- `AutoMapper` → mover para Application (já está)
- `FluentValidation` e `FluentValidation.DependencyInjectionExtensions` → manter no projeto principal se Pedidos usa

**Step 2: Atualizar Program.cs**

Principais mudanças:

1. Trocar `using` de namespaces antigos pelos novos
2. Trocar `typeof(MappingProfile)` por `typeof(ProdutoMappingProfile)`
3. Substituir registro manual de `IProdutoService` e validators por `builder.Services.AddProdutos()`
4. Registrar `IProdutoContext` → `AppDbContext` para injeção de dependência do repositório
5. Atualizar `MapProdutoEndpoints()` e `MapAuthEndpoints()` (namespace mudou mas nomes dos métodos são iguais)
6. Atualizar `DbSeeder.Seed()` para nova assinatura

```csharp
// Remover usings antigos:
// using ProdutosAPI.Produtos.DTOs;
// using ProdutosAPI.Produtos.Endpoints;
// using ProdutosAPI.Produtos.Services;
// using ProdutosAPI.Produtos.Validators;

// Adicionar novos usings:
using ProdutosAPI.Produtos.API.Endpoints;
using ProdutosAPI.Produtos.API.Extensions;
using ProdutosAPI.Produtos.Application.Interfaces;
using ProdutosAPI.Produtos.Application.Mappings;
using ProdutosAPI.Produtos.Infrastructure.Data;

// Trocar:
// builder.Services.AddScoped<IProdutoService, ProdutoService>();
// builder.Services.AddValidatorsFromAssemblyContaining<CriarProdutoValidator>();
// Por:
builder.Services.AddProdutos();

// Adicionar para conectar AppDbContext → IProdutoContext:
builder.Services.AddScoped<IProdutoContext>(sp => sp.GetRequiredService<AppDbContext>());

// Trocar:
// builder.Services.AddAutoMapper(typeof(MappingProfile));
// Por:
builder.Services.AddAutoMapper(typeof(ProdutoMappingProfile));

// No seed:
// DbSeeder.Seed(dbContext); → DbSeeder.Seed(dbContext); (mesmo chamada, mas agora aceita IProdutoContext)
// Como AppDbContext : IProdutoContext, a chamada funciona com cast implícito
```

**Step 3: Compilar o projeto principal**

```bash
dotnet build ProdutosAPI.csproj
```

Esperado: 0 erros. Pode haver warnings sobre `using` duplicados ou obsoletos — corrija-os.

---

## Task 8: Atualizar solução e projetos de teste

**Files:**

- Modify: `FacShopAPI.slnx`
- Modify: `ProdutosAPI.Tests/ProdutosAPI.Tests.csproj`
- Modify: `ProdutosAPI.Tests/Builders/ProdutoBuilder.cs`
- Modify: `ProdutosAPI.Tests/Unit/Domain/ProdutoTests.cs`
- Modify: `ProdutosAPI.Tests/Services/ProdutoServiceTests.cs`

**Step 1: Adicionar sub-projetos ao FacShopAPI.slnx**

```xml
<Solution>
  <Project Path="ProdutosAPI.csproj" />
  <Project Path="src/Produtos/Produtos.Domain/Produtos.Domain.csproj" />
  <Project Path="src/Produtos/Produtos.Application/Produtos.Application.csproj" />
  <Project Path="src/Produtos/Produtos.Infrastructure/Produtos.Infrastructure.csproj" />
  <Project Path="src/Produtos/Produtos.API/Produtos.API.csproj" />
  <Project Path="ProdutosAPI.Tests/ProdutosAPI.Tests.csproj" />
  <Project Path="Pedidos.Tests/Pedidos.Tests.csproj" />
</Solution>
```

**Step 2: Atualizar ProdutosAPI.Tests.csproj**

Adicionar referências diretas para acessar tipos internos dos sub-projetos:

```xml
<ItemGroup>
  <ProjectReference Include="../ProdutosAPI.csproj" />
  <ProjectReference Include="../src/Produtos/Produtos.Domain/Produtos.Domain.csproj" />
  <ProjectReference Include="../src/Produtos/Produtos.Application/Produtos.Application.csproj" />
  <ProjectReference Include="../src/Produtos/Produtos.Infrastructure/Produtos.Infrastructure.csproj" />
</ItemGroup>
```

**Step 3: Atualizar ProdutoBuilder.cs**

Altere o `using`:

```csharp
// Antes:
using ProdutosAPI.Produtos.Models;
// Depois:
using ProdutosAPI.Produtos.Domain;
```

**Step 4: Atualizar ProdutoTests.cs**

Altere o `using`:

```csharp
// Antes:
using ProdutosAPI.Produtos.Models;
// Depois:
using ProdutosAPI.Produtos.Domain;
using ProdutosAPI.Produtos.Domain.Common;
```

Verificar se os asserts que usam `Result` precisam do namespace novo.

**Step 5: Atualizar ProdutoServiceTests.cs**

Os testes do serviço atualmente mockam `AppDbContext` e `IMapper`. Com a nova arquitetura, `ProdutoService` recebe `IProdutoRepository` e `IMapper`. Reescreva os mocks:

```csharp
// Antes: var mockContext = new Mock<AppDbContext>(...);
// Depois: var mockRepository = new Mock<IProdutoRepository>();

// Antes: var service = new ProdutoService(mockContext.Object, mockMapper.Object, logger);
// Depois: var service = new ProdutoService(mockRepository.Object, mockMapper.Object, logger);
```

Adicione usings:

```csharp
using ProdutosAPI.Produtos.Application.Repositories;
using ProdutosAPI.Produtos.Application.Services;
using ProdutosAPI.Produtos.Application.DTOs;
using ProdutosAPI.Produtos.Domain;
```

**Step 6: Atualizar ProdutoValidatorTests.cs e ProdutoEndpointsTests.cs**

Atualizar os `using` de DTOs:

```csharp
// Antes: using ProdutosAPI.Produtos.DTOs;
// Depois: using ProdutosAPI.Produtos.Application.DTOs;
```

---

## Task 9: Build e validação completa

**Step 1: Build da solução completa**

```bash
dotnet build FacShopAPI.slnx
```

Esperado: 0 erros em todos os projetos.

**Step 2: Rodar todos os testes**

```bash
dotnet test ProdutosAPI.Tests/ProdutosAPI.Tests.csproj
```

Esperado: 112 passed, 0 failed.

```bash
dotnet test Pedidos.Tests/Pedidos.Tests.csproj
```

Esperado: todos os testes passando.

**Step 3: Verificar a aplicação sobe corretamente**

```bash
dotnet run --project ProdutosAPI.csproj
```

Esperado: `Now listening on: http://localhost:5000`. Abra http://localhost:5000 para verificar o Swagger UI.

---

## Task 10: Remover código antigo e commit

**Files:**

- Delete: `src/Produtos/DTOs/` (conteúdo migrado para Produtos.Application)
- Delete: `src/Produtos/Models/` (migrado para Produtos.Domain)
- Delete: `src/Produtos/Services/` (migrado para Produtos.Application)
- Delete: `src/Produtos/Validators/` (migrado para Produtos.Application)
- Delete: `src/Produtos/Endpoints/` (migrado para Produtos.API)
- Delete: `src/Shared/Data/DbSeeder.cs` (migrado para Produtos.Infrastructure)
- Delete: `src/Shared/Common/MappingProfile.cs` (substituído por ProdutoMappingProfile)
- Modify: `src/InternalsVisibleTo.cs` → pode ser removido se os InternalsVisibleTo estiverem nos .csproj

**Step 1: Remover diretórios antigos**

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

- Produtos.Domain: entidade Produto + Result (sem dependências externas)
- Produtos.Application: DTOs, IProdutoService, ProdutoService, IProdutoRepository,
  validadores, ProdutoMappingProfile, IProdutoContext
- Produtos.Infrastructure: EfProdutoRepository, DbSeeder
- Produtos.API: endpoints, AuthEndpoints, ProdutosServiceExtensions
- AppDbContext implementa IProdutoContext (inversão de dependência)
- Testes atualizados para novos namespaces"
```

---

## Diagrama de Dependências

```
ProdutosAPI.csproj (host)
├── src/Pedidos/ ────────────────────────────────────────── (inalterado)
├── src/Shared/Data/AppDbContext.cs ─ implements IProdutoContext
│
├── Produtos.API ───────────────────────────────────────── (ASP.NET endpoints)
│   ├── → Produtos.Application
│   └── → Produtos.Infrastructure (para DI)
│
├── Produtos.Infrastructure ──────────────────────────────  (EF Core repos)
│   ├── → Produtos.Application (IProdutoRepository, IProdutoContext)
│   └── → Produtos.Domain
│
├── Produtos.Application ─────────────────────────────────  (use cases, DTOs)
│   └── → Produtos.Domain
│
└── Produtos.Domain ──────────────────────────────────────  (entidades puras, sem deps)
```

**Fluxo de uma requisição `POST /api/v1/produtos`:**

```
HTTP → ProdutoEndpoints.cs (Produtos.API)
     → CriarProdutoValidator (Produtos.Application)
     → IProdutoService → ProdutoService (Produtos.Application)
     → IProdutoRepository → EfProdutoRepository (Produtos.Infrastructure)
     → IProdutoContext → AppDbContext (ProdutosAPI.csproj)
     → SQLite / InMemory DB
```
