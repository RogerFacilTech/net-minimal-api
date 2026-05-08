# Fase 2 â€” Novos Recursos do CatÃ¡logo

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.
> **PrÃ©-requisito:** Fase 1 concluÃ­da â€” `src/Catalogo/` existindo com Produto funcionando em `/api/v1/catalogo/produtos`.

**Goal:** Adicionar quatro novos recursos ao bounded context CatÃ¡logo: `Categorias` (domÃ­nio rico, hierarquia 2 nÃ­veis), `Variantes` (domÃ­nio rico, SKU Ãºnico), `Atributos` e `MÃ­dias` (CRUD simples).

**Architecture:** Cada recurso segue a mesma estrutura: entidade em `Catalogo.Domain`, DTOs/repos/service em `Catalogo.Application`, repos EF+Dapper em `Catalogo.Infrastructure`, endpoints em `Catalogo.API/Endpoints/{Recurso}/`. `AppDbContext` recebe novo DbSet por recurso + migration por recurso.

**Tech Stack:** .NET 10, EF Core 10 (migrations), Dapper, FluentValidation, xUnit.

---

### Task 1: Categoria â€” Domain

**Files:**
- Create: `src/Catalogo/Catalogo.Domain/Categoria.cs`

- [ ] **Step 1: Criar Categoria.cs**

Crie `src/Catalogo/Catalogo.Domain/Categoria.cs`:

```csharp
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Domain;

public class Categoria
{
    private Categoria() { }

    public int Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public int? CategoriaPaiId { get; private set; }
    public bool Ativa { get; private set; } = true;
    public DateTime DataCriacao { get; private set; }
    public DateTime DataAtualizacao { get; private set; }

    public static Result<Categoria> Criar(string nome, int? categoriaPaiId = null)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 2)
            return Result<Categoria>.Fail("Nome deve ter ao menos 2 caracteres.");
        if (nome.Length > 100)
            return Result<Categoria>.Fail("Nome nÃ£o pode exceder 100 caracteres.");

        var agora = DateTime.UtcNow;
        return Result<Categoria>.Ok(new Categoria
        {
            Nome = nome.Trim(),
            Slug = GerarSlug(nome),
            CategoriaPaiId = categoriaPaiId,
            Ativa = true,
            DataCriacao = agora,
            DataAtualizacao = agora
        });
    }

    public Result Renomear(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome) || novoNome.Length < 2)
            return Result.Fail("Nome deve ter ao menos 2 caracteres.");
        if (novoNome.Length > 100)
            return Result.Fail("Nome nÃ£o pode exceder 100 caracteres.");
        Nome = novoNome.Trim();
        Slug = GerarSlug(novoNome);
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result Desativar()
    {
        if (!Ativa)
            return Result.Fail("Categoria jÃ¡ estÃ¡ inativa.");
        Ativa = false;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    private static string GerarSlug(string nome)
    {
        var normalized = nome.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return Regex.Replace(sb.ToString().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
    }
}
```

- [ ] **Step 2: Build Domain**

```bash
dotnet build src/Catalogo/Catalogo.Domain/Catalogo.Domain.csproj
```
Esperado: `Build succeeded, 0 errors`.

---

### Task 2: Categoria â€” Application

**Files:**
- Create: `src/Catalogo/Catalogo.Application/DTOs/Categoria/CategoriaDTO.cs`
- Create: `src/Catalogo/Catalogo.Application/Repositories/ICategoriaCommandRepository.cs`
- Create: `src/Catalogo/Catalogo.Application/Repositories/ICategoriaQueryRepository.cs`
- Create: `src/Catalogo/Catalogo.Application/Services/ICategoriaService.cs`
- Create: `src/Catalogo/Catalogo.Application/Services/CategoriaService.cs`
- Create: `src/Catalogo/Catalogo.Application/Validators/CategoriaValidator.cs`
- Modify: `src/Catalogo/Catalogo.Application/Interfaces/ICatalogoContext.cs`

- [ ] **Step 1: Criar CategoriaDTO.cs**

Crie `src/Catalogo/Catalogo.Application/DTOs/Categoria/CategoriaDTO.cs`:

```csharp
namespace FacShopAPI.Catalogo.Application.DTOs.Categoria;

public class CriarCategoriaRequest
{
    public string Nome { get; set; } = string.Empty;
    public int? CategoriaPaiId { get; set; }
}

public class RenomearCategoriaRequest
{
    public string Nome { get; set; } = string.Empty;
}

public class CategoriaResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? CategoriaPaiId { get; set; }
    public bool Ativa { get; set; }
    public DateTime DataCriacao { get; set; }
    public List<CategoriaResponse> Subcategorias { get; set; } = new();
}
```

- [ ] **Step 2: Criar ICategoriaCommandRepository.cs**

Crie `src/Catalogo/Catalogo.Application/Repositories/ICategoriaCommandRepository.cs`:

```csharp
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface ICategoriaCommandRepository
{
    Task<Categoria?> ObterPorIdAsync(int id);
    Task<Categoria> AdicionarAsync(Categoria categoria);
    Task SaveChangesAsync();
    Task<bool> TemProdutosAtivosAsync(int categoriaId);
    Task<bool> CategoriaPaiTemSubcategoriasAsync(int categoriaPaiId);
}
```

- [ ] **Step 3: Criar ICategoriaQueryRepository.cs**

Crie `src/Catalogo/Catalogo.Application/Repositories/ICategoriaQueryRepository.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Categoria;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface ICategoriaQueryRepository
{
    Task<List<CategoriaResponse>> ListarRaizComSubcategoriasAsync();
    Task<CategoriaResponse?> ObterPorIdAsync(int id);
}
```

- [ ] **Step 4: Criar ICategoriaService.cs**

Crie `src/Catalogo/Catalogo.Application/Services/ICategoriaService.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Categoria;
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public interface ICategoriaService
{
    Task<List<CategoriaResponse>> ListarAsync();
    Task<CategoriaResponse?> ObterAsync(int id);
    Task<Result<CategoriaResponse>> CriarAsync(CriarCategoriaRequest request);
    Task<Result<CategoriaResponse>> RenomearAsync(int id, RenomearCategoriaRequest request);
    Task<Result> DesativarAsync(int id);
}
```

- [ ] **Step 5: Criar CategoriaService.cs**

Crie `src/Catalogo/Catalogo.Application/Services/CategoriaService.cs`:

```csharp
using Microsoft.Extensions.Logging;
using FacShopAPI.Catalogo.Application.DTOs.Categoria;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Domain;
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ICategoriaCommandRepository _commandRepo;
    private readonly ICategoriaQueryRepository _queryRepo;
    private readonly ILogger<CategoriaService> _logger;

    public CategoriaService(
        ICategoriaCommandRepository commandRepo,
        ICategoriaQueryRepository queryRepo,
        ILogger<CategoriaService> logger)
    {
        _commandRepo = commandRepo;
        _queryRepo = queryRepo;
        _logger = logger;
    }

    public Task<List<CategoriaResponse>> ListarAsync() =>
        _queryRepo.ListarRaizComSubcategoriasAsync();

    public Task<CategoriaResponse?> ObterAsync(int id) =>
        _queryRepo.ObterPorIdAsync(id);

    public async Task<Result<CategoriaResponse>> CriarAsync(CriarCategoriaRequest request)
    {
        if (request.CategoriaPaiId.HasValue)
        {
            var pai = await _commandRepo.ObterPorIdAsync(request.CategoriaPaiId.Value);
            if (pai is null)
                return Result<CategoriaResponse>.Fail("Categoria pai nÃ£o encontrada.");
            if (pai.CategoriaPaiId.HasValue)
                return Result<CategoriaResponse>.Fail(
                    "NÃ£o Ã© possÃ­vel criar subcategoria de uma subcategoria. MÃ¡ximo 2 nÃ­veis.");
        }

        var result = Categoria.Criar(request.Nome, request.CategoriaPaiId);
        if (!result.IsSuccess)
            return Result<CategoriaResponse>.Fail(result.Error!);

        var categoria = await _commandRepo.AdicionarAsync(result.Value!);
        _logger.LogInformation("Categoria criada. ID: {Id}", categoria.Id);

        return Result<CategoriaResponse>.Ok(new CategoriaResponse
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            Slug = categoria.Slug,
            CategoriaPaiId = categoria.CategoriaPaiId,
            Ativa = categoria.Ativa,
            DataCriacao = categoria.DataCriacao
        });
    }

    public async Task<Result<CategoriaResponse>> RenomearAsync(int id, RenomearCategoriaRequest request)
    {
        var categoria = await _commandRepo.ObterPorIdAsync(id);
        if (categoria is null)
            return Result<CategoriaResponse>.Fail("Categoria nÃ£o encontrada.");

        var result = categoria.Renomear(request.Nome);
        if (!result.IsSuccess)
            return Result<CategoriaResponse>.Fail(result.Error!);

        await _commandRepo.SaveChangesAsync();
        return Result<CategoriaResponse>.Ok(new CategoriaResponse
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            Slug = categoria.Slug,
            CategoriaPaiId = categoria.CategoriaPaiId,
            Ativa = categoria.Ativa,
            DataCriacao = categoria.DataCriacao
        });
    }

    public async Task<Result> DesativarAsync(int id)
    {
        var categoria = await _commandRepo.ObterPorIdAsync(id);
        if (categoria is null)
            return Result.Fail("Categoria nÃ£o encontrada.");

        var temProdutos = await _commandRepo.TemProdutosAtivosAsync(id);
        if (temProdutos)
            return Result.Fail("NÃ£o Ã© possÃ­vel desativar uma categoria com produtos ativos.");

        var result = categoria.Desativar();
        if (!result.IsSuccess) return result;

        await _commandRepo.SaveChangesAsync();
        return Result.Ok();
    }
}
```

- [ ] **Step 6: Criar CategoriaValidator.cs**

Crie `src/Catalogo/Catalogo.Application/Validators/CategoriaValidator.cs`:

```csharp
using FluentValidation;
using FacShopAPI.Catalogo.Application.DTOs.Categoria;

namespace FacShopAPI.Catalogo.Application.Validators;

public class CriarCategoriaValidator : AbstractValidator<CriarCategoriaRequest>
{
    public CriarCategoriaValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Nome Ã© obrigatÃ³rio.")
            .MinimumLength(2).WithMessage("Nome deve ter ao menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Nome nÃ£o pode exceder 100 caracteres.");
    }
}

public class RenomearCategoriaValidator : AbstractValidator<RenomearCategoriaRequest>
{
    public RenomearCategoriaValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Nome Ã© obrigatÃ³rio.")
            .MinimumLength(2).WithMessage("Nome deve ter ao menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Nome nÃ£o pode exceder 100 caracteres.");
    }
}
```

- [ ] **Step 7: Atualizar ICatalogoContext**

Adicione Categorias Ã  interface `src/Catalogo/Catalogo.Application/Interfaces/ICatalogoContext.cs`:

```csharp
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Interfaces;

public interface ICatalogoContext
{
    IQueryable<Produto> Produtos { get; }
    IQueryable<Categoria> Categorias { get; }
    void AddProduto(Produto produto);
    void AddCategoria(Categoria categoria);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 8: Build Application**

```bash
dotnet build src/Catalogo/Catalogo.Application/Catalogo.Application.csproj
```
Esperado: `Build succeeded, 0 errors`.

---

### Task 3: Categoria â€” Infrastructure + AppDbContext + Migration

**Files:**
- Create: `src/Catalogo/Catalogo.Infrastructure/Repositories/EfCategoriaCommandRepository.cs`
- Create: `src/Catalogo/Catalogo.Infrastructure/Queries/DapperCategoriaQueryRepository.cs`
- Modify: `src/Shared/Data/AppDbContext.cs`

- [ ] **Step 1: Atualizar AppDbContext**

Em `src/Shared/Data/AppDbContext.cs`, adicione:

```csharp
// DbSets
public DbSet<Categoria> Categorias => Set<Categoria>();

// ICatalogoContext
IQueryable<Categoria> ICatalogoContext.Categorias => Set<Categoria>();
public void AddCategoria(Categoria categoria) => this.Add(categoria);
```

No `OnModelCreating`, adicione configuraÃ§Ã£o da entidade Categoria:

```csharp
modelBuilder.Entity<Categoria>(entity =>
{
    entity.HasKey(c => c.Id);

    entity.Property(c => c.Nome)
        .IsRequired()
        .HasMaxLength(100)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(c => c.Slug)
        .IsRequired()
        .HasMaxLength(100)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.HasIndex(c => c.Slug)
        .IsUnique()
        .HasDatabaseName("idx_categoria_slug");

    entity.Property(c => c.CategoriaPaiId)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(c => c.Ativa)
        .HasDefaultValue(true)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(c => c.DataCriacao)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(c => c.DataAtualizacao)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.HasOne<Categoria>()
        .WithMany()
        .HasForeignKey(c => c.CategoriaPaiId)
        .OnDelete(DeleteBehavior.Restrict);
});
```

- [ ] **Step 2: Criar EfCategoriaCommandRepository.cs**

Crie `src/Catalogo/Catalogo.Infrastructure/Repositories/EfCategoriaCommandRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Infrastructure.Repositories;

public class EfCategoriaCommandRepository(ICatalogoContext context) : ICategoriaCommandRepository
{
    public Task<Categoria?> ObterPorIdAsync(int id) =>
        context.Categorias.FirstOrDefaultAsync(c => c.Id == id && c.Ativa);

    public async Task<Categoria> AdicionarAsync(Categoria categoria)
    {
        context.AddCategoria(categoria);
        await context.SaveChangesAsync();
        return categoria;
    }

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();

    public async Task<bool> TemProdutosAtivosAsync(int categoriaId) =>
        await context.Produtos.AnyAsync(p => p.Categoria.Value == categoriaId.ToString() && p.Ativo);

    public async Task<bool> CategoriaPaiTemSubcategoriasAsync(int categoriaPaiId) =>
        await context.Categorias.AnyAsync(c => c.CategoriaPaiId == categoriaPaiId && c.Ativa);
}
```

> **Nota sobre `TemProdutosAtivosAsync`:** A relaÃ§Ã£o Produto â†” Categoria hoje Ã© por string (valor da CategoriaProduto). Para verificar se hÃ¡ produtos de uma categoria, precisamos de uma abordagem diferente. Considere adicionar `CategoriaId` (int?) como coluna em Produto e fazer a FK real na Fase 2 completa, OU simplesmente deixar a validaÃ§Ã£o baseada no nome da categoria. Por ora, esta verificaÃ§Ã£o Ã© sempre `false` (nenhum produto tem FK para Categoria) e pode ser refinada apÃ³s adicionar a FK real.
>
> VersÃ£o simplificada para a POC:

```csharp
public Task<bool> TemProdutosAtivosAsync(int categoriaId) =>
    Task.FromResult(false); // TODO: adicionar FK Produto.CategoriaId na Fase 2
```

- [ ] **Step 3: Criar DapperCategoriaQueryRepository.cs**

Crie `src/Catalogo/Catalogo.Infrastructure/Queries/DapperCategoriaQueryRepository.cs`:

```csharp
using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Catalogo.Application.DTOs.Categoria;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;

namespace FacShopAPI.Catalogo.Infrastructure.Queries;

public class DapperCategoriaQueryRepository : ICategoriaQueryRepository
{
    private readonly DbContext _dbContext;

    public DapperCategoriaQueryRepository(ICatalogoContext context)
    {
        _dbContext = context as DbContext
            ?? throw new InvalidOperationException("ICatalogoContext deve ser DbContext.");
    }

    public Task<List<CategoriaResponse>> ListarRaizComSubcategoriasAsync() =>
        WithConnectionAsync(async connection =>
        {
            const string sql = @"
SELECT Id, Nome, Slug, CategoriaPaiId, Ativa, DataCriacao, DataAtualizacao
FROM Categorias
WHERE Ativa = 1
ORDER BY CategoriaPaiId NULLS FIRST, Nome;";

            var rows = (await connection.QueryAsync<CategoriaRow>(sql)).ToList();

            var raiz = rows
                .Where(r => r.CategoriaPaiId is null)
                .Select(r => new CategoriaResponse
                {
                    Id = r.Id, Nome = r.Nome, Slug = r.Slug,
                    CategoriaPaiId = r.CategoriaPaiId, Ativa = r.Ativa,
                    DataCriacao = r.DataCriacao,
                    Subcategorias = rows
                        .Where(s => s.CategoriaPaiId == r.Id)
                        .Select(s => new CategoriaResponse
                        {
                            Id = s.Id, Nome = s.Nome, Slug = s.Slug,
                            CategoriaPaiId = s.CategoriaPaiId, Ativa = s.Ativa,
                            DataCriacao = s.DataCriacao
                        }).ToList()
                }).ToList();

            return raiz;
        });

    public Task<CategoriaResponse?> ObterPorIdAsync(int id) =>
        WithConnectionAsync(async connection =>
        {
            const string sql = @"
SELECT Id, Nome, Slug, CategoriaPaiId, Ativa, DataCriacao, DataAtualizacao
FROM Categorias WHERE Id = @Id AND Ativa = 1;";

            const string subSql = @"
SELECT Id, Nome, Slug, CategoriaPaiId, Ativa, DataCriacao
FROM Categorias WHERE CategoriaPaiId = @Id AND Ativa = 1;";

            var row = await connection.QuerySingleOrDefaultAsync<CategoriaRow>(sql, new { Id = id });
            if (row is null) return null;

            var subs = await connection.QueryAsync<CategoriaRow>(subSql, new { Id = id });

            return new CategoriaResponse
            {
                Id = row.Id, Nome = row.Nome, Slug = row.Slug,
                CategoriaPaiId = row.CategoriaPaiId, Ativa = row.Ativa,
                DataCriacao = row.DataCriacao,
                Subcategorias = subs.Select(s => new CategoriaResponse
                {
                    Id = s.Id, Nome = s.Nome, Slug = s.Slug,
                    CategoriaPaiId = s.CategoriaPaiId, Ativa = s.Ativa,
                    DataCriacao = s.DataCriacao
                }).ToList()
            };
        });

    private async Task<T> WithConnectionAsync<T>(Func<DbConnection, Task<T>> action)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync();
        try { return await action(connection); }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private sealed class CategoriaRow
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int? CategoriaPaiId { get; set; }
        public bool Ativa { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }
    }
}
```

- [ ] **Step 4: Criar migration para Categorias**

```bash
dotnet ef migrations add AddCategorias --project FacShopAPI.csproj
```
Esperado: arquivo de migration gerado em `Migrations/`.

- [ ] **Step 5: Build Infrastructure**

```bash
dotnet build src/Catalogo/Catalogo.Infrastructure/Catalogo.Infrastructure.csproj
```
Esperado: `Build succeeded, 0 errors`.

---

### Task 4: Categoria â€” API Endpoints

**Files:**
- Create: `src/Catalogo/Catalogo.API/Endpoints/Categorias/CategoriaEndpoints.cs`
- Modify: `src/Catalogo/Catalogo.API/Extensions/CatalogoServiceExtensions.cs`
- Modify: `Program.cs`

- [ ] **Step 1: Criar CategoriaEndpoints.cs**

Crie `src/Catalogo/Catalogo.API/Endpoints/Categorias/CategoriaEndpoints.cs`:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FacShopAPI.Catalogo.API.DTOs;
using FacShopAPI.Catalogo.Application.DTOs.Categoria;
using FacShopAPI.Catalogo.Application.Services;

namespace FacShopAPI.Catalogo.API.Endpoints.Categorias;

public static class CategoriaEndpoints
{
    public static void MapCategoriaEndpoints(this RouteGroupBuilder catalogoGroup)
    {
        var group = catalogoGroup.MapGroup("/categorias")
            .WithTags("CatÃ¡logo - Categorias");

        group.MapGet("/", ListarCategorias).WithName("ListarCategorias")
            .Produces<List<CategoriaResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous();

        group.MapGet("/{id:int}", ObterCategoria).WithName("ObterCategoria")
            .Produces<CategoriaResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        group.MapPost("/", CriarCategoria).WithName("CriarCategoria")
            .Accepts<CriarCategoriaRequest>("application/json")
            .Produces<CategoriaResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        group.MapPut("/{id:int}", RenomearCategoria).WithName("RenomearCategoria")
            .Accepts<RenomearCategoriaRequest>("application/json")
            .Produces<CategoriaResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapDelete("/{id:int}", DesativarCategoria).WithName("DesativarCategoria")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();
    }

    private static async Task<IResult> ListarCategorias(ICategoriaService service)
    {
        var categorias = await service.ListarAsync();
        return Results.Ok(categorias);
    }

    private static async Task<IResult> ObterCategoria(int id, ICategoriaService service)
    {
        var categoria = await service.ObterAsync(id);
        if (categoria is null)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404, Title = "Categoria nÃ£o encontrada",
                Detail = $"Categoria com ID {id} nÃ£o encontrada.",
                Instance = $"/api/v1/catalogo/categorias/{id}"
            });
        return Results.Ok(categoria);
    }

    private static async Task<IResult> CriarCategoria(
        CriarCategoriaRequest request, ICategoriaService service,
        IValidator<CriarCategoriaRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422, Title = "ValidaÃ§Ã£o falhou",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))
            });

        var result = await service.CriarAsync(request);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422, Title = "Regra de negÃ³cio violada", Detail = result.Error!
            });

        return Results.Created($"/api/v1/catalogo/categorias/{result.Value!.Id}", result.Value);
    }

    private static async Task<IResult> RenomearCategoria(
        int id, RenomearCategoriaRequest request, ICategoriaService service,
        IValidator<RenomearCategoriaRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422, Title = "ValidaÃ§Ã£o falhou",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))
            });

        var result = await service.RenomearAsync(id, request);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404, Title = "Categoria nÃ£o encontrada", Detail = result.Error!
            });

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> DesativarCategoria(int id, ICategoriaService service)
    {
        var result = await service.DesativarAsync(id);
        if (!result.IsSuccess)
            return result.Error!.Contains("nÃ£o encontrada")
                ? Results.NotFound(new ErrorResponse { Status = 404, Title = "Categoria nÃ£o encontrada", Detail = result.Error })
                : Results.UnprocessableEntity(new ErrorResponse { Status = 422, Title = "OperaÃ§Ã£o nÃ£o permitida", Detail = result.Error });

        return Results.NoContent();
    }
}
```

- [ ] **Step 2: Atualizar CatalogoServiceExtensions**

Em `src/Catalogo/Catalogo.API/Extensions/CatalogoServiceExtensions.cs`, adicione:

```csharp
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Application.Services;
using FacShopAPI.Catalogo.Application.Validators;
using FacShopAPI.Catalogo.Infrastructure.Queries;
using FacShopAPI.Catalogo.Infrastructure.Repositories;
// ... outros usings existentes

public static IServiceCollection AddCatalogo(this IServiceCollection services)
{
    // Produto (jÃ¡ existente)
    services.AddScoped<IProdutoService, ProdutoService>();
    services.AddScoped<IProdutoQueryRepository, DapperProdutoQueryRepository>();
    services.AddScoped<IProdutoCommandRepository, EfProdutoCommandRepository>();

    // Categoria (novo)
    services.AddScoped<ICategoriaService, CategoriaService>();
    services.AddScoped<ICategoriaQueryRepository, DapperCategoriaQueryRepository>();
    services.AddScoped<ICategoriaCommandRepository, EfCategoriaCommandRepository>();

    services.AddValidatorsFromAssemblyContaining<CriarProdutoValidator>();
    return services;
}
```

- [ ] **Step 3: Registrar endpoint em Program.cs**

Adicione apÃ³s `catalogo.MapProdutoEndpoints()`:

```csharp
catalogo.MapCategoriaEndpoints();
```

Adicione o `using` necessÃ¡rio:

```csharp
using FacShopAPI.Catalogo.API.Endpoints.Categorias;
```

- [ ] **Step 4: Build do projeto principal**

```bash
dotnet build FacShopAPI.csproj
```
Esperado: `Build succeeded, 0 errors`.

---

### Task 5: Categoria â€” Testes

**Files:**
- Create: `tests/FacShopAPI.Tests/Unit/Domain/CategoriaTests.cs`
- Create: `tests/FacShopAPI.Tests/Integration/Catalogo/CategoriaEndpointsTests.cs`

- [ ] **Step 1: Escrever testes de domÃ­nio**

Crie `tests/FacShopAPI.Tests/Unit/Domain/CategoriaTests.cs`:

```csharp
using FluentAssertions;
using FacShopAPI.Catalogo.Domain;
using Xunit;

namespace FacShopAPI.Tests.Unit.Domain;

public class CategoriaTests
{
    [Fact]
    public void Criar_NomeValido_RetornaSucesso()
    {
        var result = Categoria.Criar("EletrÃ´nicos");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Nome.Should().Be("EletrÃ´nicos");
        result.Value.Slug.Should().Be("eletronicos");
        result.Value.Ativa.Should().BeTrue();
        result.Value.CategoriaPaiId.Should().BeNull();
    }

    [Fact]
    public void Criar_ComCategoriaPaiId_DefineCategoriaPai()
    {
        var result = Categoria.Criar("Notebooks", categoriaPaiId: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CategoriaPaiId.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void Criar_NomeInvalido_RetornaFalha(string nome)
    {
        var result = Categoria.Criar(nome);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Criar_NomeMuitoLongo_RetornaFalha()
    {
        var result = Categoria.Criar(new string('A', 101));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Renomear_NomeValido_AtualizaNomeESlug()
    {
        var categoria = Categoria.Criar("EletrÃ´nicos").Value!;

        var result = categoria.Renomear("Computadores & PerifÃ©ricos");

        result.IsSuccess.Should().BeTrue();
        categoria.Nome.Should().Be("Computadores & PerifÃ©ricos");
        categoria.Slug.Should().Be("computadores-perifericos");
    }

    [Fact]
    public void Desativar_CategoriaAtiva_DesativaComSucesso()
    {
        var categoria = Categoria.Criar("EletrÃ´nicos").Value!;

        var result = categoria.Desativar();

        result.IsSuccess.Should().BeTrue();
        categoria.Ativa.Should().BeFalse();
    }

    [Fact]
    public void Desativar_CategoriaJaInativa_RetornaFalha()
    {
        var categoria = Categoria.Criar("EletrÃ´nicos").Value!;
        categoria.Desativar();

        var result = categoria.Desativar();

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("CafÃ© & ChÃ¡", "cafe-cha")]
    [InlineData("Roupas Femininas", "roupas-femininas")]
    [InlineData("  Livros  ", "livros")]
    public void Criar_SlugGeradoCorretamente(string nome, string slugEsperado)
    {
        var result = Categoria.Criar(nome);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Slug.Should().Be(slugEsperado);
    }
}
```

- [ ] **Step 2: Rodar testes de domÃ­nio**

```bash
dotnet test tests/FacShopAPI.Tests/FacShopAPI.Tests.csproj \
    --filter "FullyQualifiedName~CategoriaTests" -v minimal
```
Esperado: todos os testes passando.

- [ ] **Step 3: Escrever testes de integraÃ§Ã£o**

Crie `tests/FacShopAPI.Tests/Integration/Catalogo/CategoriaEndpointsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using FacShopAPI.Catalogo.Application.DTOs.Categoria;
using FacShopAPI.Tests.Integration;
using Xunit;

namespace FacShopAPI.Tests.Integration.Catalogo;

public class CategoriaEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CategoriaEndpointsTests(ApiFactory factory) => _factory = factory;

    private HttpClient CriarCliente() => _factory.CreateClient();

    private async Task<HttpClient> CriarClienteAutenticadoAsync()
    {
        var client = _factory.CreateClient();
        var token = await AuthHelper.ObterTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task GET_Categorias_SemAutenticacao_Retorna200()
    {
        var client = CriarCliente();

        var response = await client.GetAsync("/api/v1/catalogo/categorias");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CategoriaResponse>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_CriarCategoria_Valida_Retorna201()
    {
        var client = await CriarClienteAutenticadoAsync();

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "EletrÃ´nicos Teste" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CategoriaResponse>();
        result!.Nome.Should().Be("EletrÃ´nicos Teste");
        result.Slug.Should().Be("eletronicos-teste");
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_CriarSubcategoria_ComPaiExistente_Retorna201()
    {
        var client = await CriarClienteAutenticadoAsync();

        // Criar categoria pai
        var paiResponse = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Categoria Pai" });
        var pai = await paiResponse.Content.ReadFromJsonAsync<CategoriaResponse>();

        // Criar subcategoria
        var subResponse = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Subcategoria", CategoriaPaiId = pai!.Id });

        subResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sub = await subResponse.Content.ReadFromJsonAsync<CategoriaResponse>();
        sub!.CategoriaPaiId.Should().Be(pai.Id);
    }

    [Fact]
    public async Task POST_CriarSubcategoriaDeSubcategoria_Retorna422()
    {
        var client = await CriarClienteAutenticadoAsync();

        var paiResp = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Pai Max Nivel" });
        var pai = await paiResp.Content.ReadFromJsonAsync<CategoriaResponse>();

        var filhaResp = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Filha", CategoriaPaiId = pai!.Id });
        var filha = await filhaResp.Content.ReadFromJsonAsync<CategoriaResponse>();

        // Tenta criar neto â€” deve falhar
        var netoResp = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Neto", CategoriaPaiId = filha!.Id });

        netoResp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_CriarCategoria_NomeVazio_Retorna422()
    {
        var client = await CriarClienteAutenticadoAsync();

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DELETE_DesativarCategoria_Existente_Retorna204()
    {
        var client = await CriarClienteAutenticadoAsync();

        var criarResp = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Para Deletar" });
        var cat = await criarResp.Content.ReadFromJsonAsync<CategoriaResponse>();

        var deleteResp = await client.DeleteAsync($"/api/v1/catalogo/categorias/{cat!.Id}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_ObterCategoria_Inexistente_Retorna404()
    {
        var client = CriarCliente();

        var response = await client.GetAsync("/api/v1/catalogo/categorias/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 4: Rodar testes de integraÃ§Ã£o**

```bash
dotnet test tests/FacShopAPI.Tests/FacShopAPI.Tests.csproj \
    --filter "FullyQualifiedName~CategoriaEndpointsTests" -v minimal
```
Esperado: todos os testes passando.

- [ ] **Step 5: Commit Categoria**

```bash
git add -A
git commit -m "feat: adicionar recurso Categoria ao CatÃ¡logo

- Categoria.cs com hierarquia de 2 nÃ­veis e geraÃ§Ã£o automÃ¡tica de slug
- CQRS: EfCategoriaCommandRepository + DapperCategoriaQueryRepository
- CategoriaService valida: mÃ¡x 2 nÃ­veis hierÃ¡rquicos, produtos ativos
- 5 endpoints em /api/v1/catalogo/categorias
- Migration AddCategorias
- Testes: domÃ­nio (7) + integraÃ§Ã£o (7)"
```

---

### Task 6: Variante â€” Domain

**Files:**
- Create: `src/Catalogo/Catalogo.Domain/ValueObjects/SKU.cs`
- Create: `src/Catalogo/Catalogo.Domain/Variante.cs`

- [ ] **Step 1: Criar SKU.cs**

Crie `src/Catalogo/Catalogo.Domain/ValueObjects/SKU.cs`:

```csharp
using System.Text.RegularExpressions;
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Domain.ValueObjects;

public sealed record SKU
{
    private static readonly Regex FormatoValido = new(@"^[A-Z0-9\-]+$", RegexOptions.Compiled);

    private SKU(string valor) => Valor = valor;
    public string Valor { get; }

    public static Result<SKU> Criar(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return Result<SKU>.Fail("SKU nÃ£o pode ser vazio.");
        valor = valor.Trim().ToUpperInvariant();
        if (valor.Length < 6 || valor.Length > 20)
            return Result<SKU>.Fail("SKU deve ter entre 6 e 20 caracteres.");
        if (!FormatoValido.IsMatch(valor))
            return Result<SKU>.Fail("SKU deve conter apenas letras maiÃºsculas, nÃºmeros e hÃ­fens.");
        return Result<SKU>.Ok(new SKU(valor));
    }

    public static SKU Reconstituir(string valor) => new(valor);
    public override string ToString() => Valor;
}
```

- [ ] **Step 2: Criar Variante.cs**

Crie `src/Catalogo/Catalogo.Domain/Variante.cs`:

```csharp
using FacShopAPI.Catalogo.Domain.Common;
using FacShopAPI.Catalogo.Domain.ValueObjects;

namespace FacShopAPI.Catalogo.Domain;

public class Variante
{
    private Variante() { }

    public int Id { get; private set; }
    public int ProdutoId { get; private set; }
    public SKU Sku { get; private set; } = null!;
    public string Descricao { get; private set; } = "";
    public PrecoProduto PrecoAdicional { get; private set; } = null!;
    public EstoqueProduto Estoque { get; private set; } = null!;
    public bool Ativa { get; private set; } = true;
    public DateTime DataCriacao { get; private set; }
    public DateTime DataAtualizacao { get; private set; }

    public static Result<Variante> Criar(
        int produtoId, string sku, string descricao,
        decimal precoAdicional, int estoque)
    {
        if (produtoId <= 0)
            return Result<Variante>.Fail("ProdutoId invÃ¡lido.");
        if (string.IsNullOrWhiteSpace(descricao) || descricao.Length > 200)
            return Result<Variante>.Fail("DescriÃ§Ã£o deve ter entre 1 e 200 caracteres.");

        var skuResult = SKU.Criar(sku);
        if (!skuResult.IsSuccess)
            return Result<Variante>.Fail(skuResult.Error!);

        var precoResult = PrecoProduto.Criar(precoAdicional);
        if (!precoResult.IsSuccess)
            return Result<Variante>.Fail(precoResult.Error!);

        var estoqueResult = EstoqueProduto.Criar(estoque);
        if (!estoqueResult.IsSuccess)
            return Result<Variante>.Fail(estoqueResult.Error!);

        var agora = DateTime.UtcNow;
        return Result<Variante>.Ok(new Variante
        {
            ProdutoId = produtoId,
            Sku = skuResult.Value!,
            Descricao = descricao.Trim(),
            PrecoAdicional = precoResult.Value!,
            Estoque = estoqueResult.Value!,
            Ativa = true,
            DataCriacao = agora,
            DataAtualizacao = agora
        });
    }

    public Result AtualizarPreco(decimal novoPrecoAdicional)
    {
        var result = PrecoProduto.Criar(novoPrecoAdicional);
        if (!result.IsSuccess) return Result.Fail(result.Error!);
        PrecoAdicional = result.Value!;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result AtualizarEstoque(int quantidade)
    {
        var result = EstoqueProduto.Criar(quantidade);
        if (!result.IsSuccess) return Result.Fail(result.Error!);
        Estoque = result.Value!;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result Desativar()
    {
        if (!Ativa) return Result.Fail("Variante jÃ¡ estÃ¡ inativa.");
        Ativa = false;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }
}
```

- [ ] **Step 3: Build Domain**

```bash
dotnet build src/Catalogo/Catalogo.Domain/Catalogo.Domain.csproj
```
Esperado: `Build succeeded, 0 errors`.

---

### Task 7: Variante â€” Application + Infrastructure + API + Tests

**Files:**
- Create: `src/Catalogo/Catalogo.Application/DTOs/Variante/VarianteDTO.cs`
- Create: `src/Catalogo/Catalogo.Application/Repositories/IVarianteCommandRepository.cs`
- Create: `src/Catalogo/Catalogo.Application/Repositories/IVarianteQueryRepository.cs`
- Create: `src/Catalogo/Catalogo.Application/Services/IVarianteService.cs`
- Create: `src/Catalogo/Catalogo.Application/Services/VarianteService.cs`
- Create: `src/Catalogo/Catalogo.Application/Validators/VarianteValidator.cs`
- Create: `src/Catalogo/Catalogo.Infrastructure/Repositories/EfVarianteCommandRepository.cs`
- Create: `src/Catalogo/Catalogo.Infrastructure/Queries/DapperVarianteQueryRepository.cs`
- Create: `src/Catalogo/Catalogo.API/Endpoints/Variantes/VarianteEndpoints.cs`
- Create: `tests/FacShopAPI.Tests/Unit/Domain/VarianteTests.cs`
- Create: `tests/FacShopAPI.Tests/Integration/Catalogo/VarianteEndpointsTests.cs`
- Modify: `src/Catalogo/Catalogo.Application/Interfaces/ICatalogoContext.cs`
- Modify: `src/Shared/Data/AppDbContext.cs`
- Modify: `src/Catalogo/Catalogo.API/Extensions/CatalogoServiceExtensions.cs`
- Modify: `Program.cs`

- [ ] **Step 1: Criar VarianteDTO.cs**

Crie `src/Catalogo/Catalogo.Application/DTOs/Variante/VarianteDTO.cs`:

```csharp
namespace FacShopAPI.Catalogo.Application.DTOs.Variante;

public class CriarVarianteRequest
{
    public int ProdutoId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoAdicional { get; set; }
    public int Estoque { get; set; }
}

public class AtualizarPrecoVarianteRequest
{
    public decimal PrecoAdicional { get; set; }
}

public class AtualizarEstoqueVarianteRequest
{
    public int Estoque { get; set; }
}

public class VarianteResponse
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoAdicional { get; set; }
    public int Estoque { get; set; }
    public bool Ativa { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}
```

- [ ] **Step 2: Criar IVarianteCommandRepository.cs e IVarianteQueryRepository.cs**

Crie `src/Catalogo/Catalogo.Application/Repositories/IVarianteCommandRepository.cs`:

```csharp
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IVarianteCommandRepository
{
    Task<Variante?> ObterPorIdAsync(int id);
    Task<Variante> AdicionarAsync(Variante variante);
    Task SaveChangesAsync();
    Task<bool> SkuExisteParaProdutoAsync(int produtoId, string sku);
}
```

Crie `src/Catalogo/Catalogo.Application/Repositories/IVarianteQueryRepository.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Variante;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IVarianteQueryRepository
{
    Task<List<VarianteResponse>> ListarPorProdutoAsync(int produtoId);
    Task<VarianteResponse?> ObterPorIdAsync(int id);
}
```

- [ ] **Step 3: Criar IVarianteService.cs e VarianteService.cs**

Crie `src/Catalogo/Catalogo.Application/Services/IVarianteService.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Variante;
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public interface IVarianteService
{
    Task<List<VarianteResponse>> ListarPorProdutoAsync(int produtoId);
    Task<VarianteResponse?> ObterAsync(int id);
    Task<Result<VarianteResponse>> CriarAsync(CriarVarianteRequest request);
    Task<Result<VarianteResponse>> AtualizarPrecoAsync(int id, AtualizarPrecoVarianteRequest request);
    Task<Result<VarianteResponse>> AtualizarEstoqueAsync(int id, AtualizarEstoqueVarianteRequest request);
    Task<Result> DesativarAsync(int id);
}
```

Crie `src/Catalogo/Catalogo.Application/Services/VarianteService.cs`:

```csharp
using Microsoft.Extensions.Logging;
using FacShopAPI.Catalogo.Application.DTOs.Variante;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Domain;
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public class VarianteService : IVarianteService
{
    private readonly IVarianteCommandRepository _commandRepo;
    private readonly IVarianteQueryRepository _queryRepo;
    private readonly ILogger<VarianteService> _logger;

    public VarianteService(
        IVarianteCommandRepository commandRepo,
        IVarianteQueryRepository queryRepo,
        ILogger<VarianteService> logger)
    {
        _commandRepo = commandRepo;
        _queryRepo = queryRepo;
        _logger = logger;
    }

    public Task<List<VarianteResponse>> ListarPorProdutoAsync(int produtoId) =>
        _queryRepo.ListarPorProdutoAsync(produtoId);

    public Task<VarianteResponse?> ObterAsync(int id) => _queryRepo.ObterPorIdAsync(id);

    public async Task<Result<VarianteResponse>> CriarAsync(CriarVarianteRequest request)
    {
        var skuExiste = await _commandRepo.SkuExisteParaProdutoAsync(request.ProdutoId, request.Sku);
        if (skuExiste)
            return Result<VarianteResponse>.Fail($"SKU '{request.Sku}' jÃ¡ existe para este produto.");

        var result = Variante.Criar(request.ProdutoId, request.Sku, request.Descricao,
            request.PrecoAdicional, request.Estoque);
        if (!result.IsSuccess) return Result<VarianteResponse>.Fail(result.Error!);

        var variante = await _commandRepo.AdicionarAsync(result.Value!);
        _logger.LogInformation("Variante criada. ID: {Id}", variante.Id);
        return Result<VarianteResponse>.Ok(MapToResponse(variante));
    }

    public async Task<Result<VarianteResponse>> AtualizarPrecoAsync(int id, AtualizarPrecoVarianteRequest request)
    {
        var variante = await _commandRepo.ObterPorIdAsync(id);
        if (variante is null) return Result<VarianteResponse>.Fail("Variante nÃ£o encontrada.");
        var r = variante.AtualizarPreco(request.PrecoAdicional);
        if (!r.IsSuccess) return Result<VarianteResponse>.Fail(r.Error!);
        await _commandRepo.SaveChangesAsync();
        return Result<VarianteResponse>.Ok(MapToResponse(variante));
    }

    public async Task<Result<VarianteResponse>> AtualizarEstoqueAsync(int id, AtualizarEstoqueVarianteRequest request)
    {
        var variante = await _commandRepo.ObterPorIdAsync(id);
        if (variante is null) return Result<VarianteResponse>.Fail("Variante nÃ£o encontrada.");
        var r = variante.AtualizarEstoque(request.Estoque);
        if (!r.IsSuccess) return Result<VarianteResponse>.Fail(r.Error!);
        await _commandRepo.SaveChangesAsync();
        return Result<VarianteResponse>.Ok(MapToResponse(variante));
    }

    public async Task<Result> DesativarAsync(int id)
    {
        var variante = await _commandRepo.ObterPorIdAsync(id);
        if (variante is null) return Result.Fail("Variante nÃ£o encontrada.");
        var r = variante.Desativar();
        if (!r.IsSuccess) return r;
        await _commandRepo.SaveChangesAsync();
        return Result.Ok();
    }

    private static VarianteResponse MapToResponse(Variante v) => new()
    {
        Id = v.Id, ProdutoId = v.ProdutoId, Sku = v.Sku.Valor,
        Descricao = v.Descricao, PrecoAdicional = v.PrecoAdicional.Value,
        Estoque = v.Estoque.Value, Ativa = v.Ativa,
        DataCriacao = v.DataCriacao, DataAtualizacao = v.DataAtualizacao
    };
}
```

- [ ] **Step 4: Criar VarianteValidator.cs**

Crie `src/Catalogo/Catalogo.Application/Validators/VarianteValidator.cs`:

```csharp
using FluentValidation;
using FacShopAPI.Catalogo.Application.DTOs.Variante;

namespace FacShopAPI.Catalogo.Application.Validators;

public class CriarVarianteValidator : AbstractValidator<CriarVarianteRequest>
{
    public CriarVarianteValidator()
    {
        RuleFor(v => v.ProdutoId).GreaterThan(0).WithMessage("ProdutoId invÃ¡lido.");
        RuleFor(v => v.Sku).NotEmpty().WithMessage("SKU Ã© obrigatÃ³rio.")
            .MinimumLength(6).WithMessage("SKU deve ter ao menos 6 caracteres.")
            .MaximumLength(20).WithMessage("SKU nÃ£o pode exceder 20 caracteres.")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("SKU deve conter apenas letras maiÃºsculas, nÃºmeros e hÃ­fens.");
        RuleFor(v => v.Descricao).NotEmpty().WithMessage("DescriÃ§Ã£o Ã© obrigatÃ³ria.")
            .MaximumLength(200).WithMessage("DescriÃ§Ã£o nÃ£o pode exceder 200 caracteres.");
        RuleFor(v => v.PrecoAdicional).GreaterThan(0).WithMessage("PreÃ§o adicional deve ser maior que zero.");
        RuleFor(v => v.Estoque).GreaterThanOrEqualTo(0).WithMessage("Estoque nÃ£o pode ser negativo.");
    }
}
```

- [ ] **Step 5: Atualizar ICatalogoContext com Variantes**

Em `src/Catalogo/Catalogo.Application/Interfaces/ICatalogoContext.cs`:

```csharp
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Interfaces;

public interface ICatalogoContext
{
    IQueryable<Produto> Produtos { get; }
    IQueryable<Categoria> Categorias { get; }
    IQueryable<Variante> Variantes { get; }
    void AddProduto(Produto produto);
    void AddCategoria(Categoria categoria);
    void AddVariante(Variante variante);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 6: Atualizar AppDbContext com Variantes**

Em `src/Shared/Data/AppDbContext.cs`, adicione:

```csharp
public DbSet<Variante> Variantes => Set<Variante>();
IQueryable<Variante> ICatalogoContext.Variantes => Set<Variante>();
public void AddVariante(Variante variante) => this.Add(variante);
```

No `OnModelCreating`, adicione:

```csharp
modelBuilder.Entity<Variante>(entity =>
{
    entity.HasKey(v => v.Id);

    entity.Property(v => v.ProdutoId).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(v => v.Sku)
        .IsRequired()
        .HasMaxLength(20)
        .HasConversion(sku => sku.Valor, valor => SKU.Reconstituir(valor))
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.HasIndex(v => new { v.ProdutoId, v.Sku })
        .IsUnique()
        .HasDatabaseName("idx_variante_produto_sku");

    entity.Property(v => v.Descricao).IsRequired().HasMaxLength(200)
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(v => v.PrecoAdicional)
        .HasPrecision(10, 2)
        .HasConversion(p => p.Value, v => PrecoProduto.Reconstituir(v))
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(v => v.Estoque)
        .HasConversion(e => e.Value, v => EstoqueProduto.Reconstituir(v))
        .UsePropertyAccessMode(PropertyAccessMode.Property);

    entity.Property(v => v.Ativa).HasDefaultValue(true).UsePropertyAccessMode(PropertyAccessMode.Property);
    entity.Property(v => v.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
    entity.Property(v => v.DataAtualizacao).UsePropertyAccessMode(PropertyAccessMode.Property);
});
```

- [ ] **Step 7: Criar EfVarianteCommandRepository.cs**

Crie `src/Catalogo/Catalogo.Infrastructure/Repositories/EfVarianteCommandRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Infrastructure.Repositories;

public class EfVarianteCommandRepository(ICatalogoContext context) : IVarianteCommandRepository
{
    public Task<Variante?> ObterPorIdAsync(int id) =>
        context.Variantes.FirstOrDefaultAsync(v => v.Id == id && v.Ativa);

    public async Task<Variante> AdicionarAsync(Variante variante)
    {
        context.AddVariante(variante);
        await context.SaveChangesAsync();
        return variante;
    }

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();

    public async Task<bool> SkuExisteParaProdutoAsync(int produtoId, string sku) =>
        await context.Variantes.AnyAsync(v => v.ProdutoId == produtoId && v.Sku.Valor == sku && v.Ativa);
}
```

- [ ] **Step 8: Criar DapperVarianteQueryRepository.cs**

Crie `src/Catalogo/Catalogo.Infrastructure/Queries/DapperVarianteQueryRepository.cs`:

```csharp
using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Catalogo.Application.DTOs.Variante;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;

namespace FacShopAPI.Catalogo.Infrastructure.Queries;

public class DapperVarianteQueryRepository : IVarianteQueryRepository
{
    private readonly DbContext _dbContext;

    public DapperVarianteQueryRepository(ICatalogoContext context)
    {
        _dbContext = context as DbContext
            ?? throw new InvalidOperationException("ICatalogoContext deve ser DbContext.");
    }

    public Task<List<VarianteResponse>> ListarPorProdutoAsync(int produtoId) =>
        WithConnectionAsync(async connection =>
        {
            const string sql = @"
SELECT Id, ProdutoId, Sku, Descricao, PrecoAdicional, Estoque, Ativa, DataCriacao, DataAtualizacao
FROM Variantes WHERE ProdutoId = @ProdutoId AND Ativa = 1 ORDER BY Sku;";
            var rows = await connection.QueryAsync<VarianteRow>(sql, new { ProdutoId = produtoId });
            return rows.Select(MapToDto).ToList();
        });

    public Task<VarianteResponse?> ObterPorIdAsync(int id) =>
        WithConnectionAsync(async connection =>
        {
            const string sql = @"
SELECT Id, ProdutoId, Sku, Descricao, PrecoAdicional, Estoque, Ativa, DataCriacao, DataAtualizacao
FROM Variantes WHERE Id = @Id AND Ativa = 1;";
            var row = await connection.QuerySingleOrDefaultAsync<VarianteRow>(sql, new { Id = id });
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

    private static VarianteResponse MapToDto(VarianteRow row) => new()
    {
        Id = row.Id, ProdutoId = row.ProdutoId, Sku = row.Sku, Descricao = row.Descricao,
        PrecoAdicional = row.PrecoAdicional, Estoque = row.Estoque, Ativa = row.Ativa,
        DataCriacao = row.DataCriacao, DataAtualizacao = row.DataAtualizacao
    };

    private sealed class VarianteRow
    {
        public int Id { get; set; }
        public int ProdutoId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal PrecoAdicional { get; set; }
        public int Estoque { get; set; }
        public bool Ativa { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }
    }
}
```

- [ ] **Step 9: Criar VarianteEndpoints.cs**

Crie `src/Catalogo/Catalogo.API/Endpoints/Variantes/VarianteEndpoints.cs`:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FacShopAPI.Catalogo.API.DTOs;
using FacShopAPI.Catalogo.Application.DTOs.Variante;
using FacShopAPI.Catalogo.Application.Services;

namespace FacShopAPI.Catalogo.API.Endpoints.Variantes;

public static class VarianteEndpoints
{
    public static void MapVarianteEndpoints(this RouteGroupBuilder catalogoGroup)
    {
        var group = catalogoGroup.MapGroup("/variantes")
            .WithTags("CatÃ¡logo - Variantes");

        group.MapGet("/", ListarVariantes).WithName("ListarVariantes")
            .Produces<List<VarianteResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous();

        group.MapGet("/{id:int}", ObterVariante).WithName("ObterVariante")
            .Produces<VarianteResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        group.MapPost("/", CriarVariante).WithName("CriarVariante")
            .Accepts<CriarVarianteRequest>("application/json")
            .Produces<VarianteResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        group.MapPut("/{id:int}", AtualizarPreco).WithName("AtualizarPrecoVariante")
            .Accepts<AtualizarPrecoVarianteRequest>("application/json")
            .Produces<VarianteResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapPatch("/{id:int}/estoque", AtualizarEstoque).WithName("AtualizarEstoqueVariante")
            .Accepts<AtualizarEstoqueVarianteRequest>("application/json")
            .Produces<VarianteResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapDelete("/{id:int}", DesativarVariante).WithName("DesativarVariante")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    private static async Task<IResult> ListarVariantes(IVarianteService service, int produtoId)
    {
        var variantes = await service.ListarPorProdutoAsync(produtoId);
        return Results.Ok(variantes);
    }

    private static async Task<IResult> ObterVariante(int id, IVarianteService service)
    {
        var variante = await service.ObterAsync(id);
        if (variante is null)
            return Results.NotFound(new ErrorResponse
            { Status = 404, Title = "Variante nÃ£o encontrada", Detail = $"Variante {id} nÃ£o encontrada." });
        return Results.Ok(variante);
    }

    private static async Task<IResult> CriarVariante(
        CriarVarianteRequest request, IVarianteService service, IValidator<CriarVarianteRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            { Status = 422, Title = "ValidaÃ§Ã£o falhou",
              Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)) });

        var result = await service.CriarAsync(request);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new ErrorResponse
            { Status = 422, Title = "Regra de negÃ³cio violada", Detail = result.Error! });

        return Results.Created($"/api/v1/catalogo/variantes/{result.Value!.Id}", result.Value);
    }

    private static async Task<IResult> AtualizarPreco(int id, AtualizarPrecoVarianteRequest request, IVarianteService service)
    {
        var result = await service.AtualizarPrecoAsync(id, request);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "Variante nÃ£o encontrada", Detail = result.Error! });
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> AtualizarEstoque(int id, AtualizarEstoqueVarianteRequest request, IVarianteService service)
    {
        var result = await service.AtualizarEstoqueAsync(id, request);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "Variante nÃ£o encontrada", Detail = result.Error! });
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> DesativarVariante(int id, IVarianteService service)
    {
        var result = await service.DesativarAsync(id);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "Variante nÃ£o encontrada", Detail = result.Error! });
        return Results.NoContent();
    }
}
```

- [ ] **Step 10: Atualizar CatalogoServiceExtensions e Program.cs**

Em `CatalogoServiceExtensions.cs`, adicione:

```csharp
services.AddScoped<IVarianteService, VarianteService>();
services.AddScoped<IVarianteQueryRepository, DapperVarianteQueryRepository>();
services.AddScoped<IVarianteCommandRepository, EfVarianteCommandRepository>();
```

Em `Program.cs`, adicione apÃ³s `catalogo.MapCategoriaEndpoints()`:

```csharp
using FacShopAPI.Catalogo.API.Endpoints.Variantes;
// ...
catalogo.MapVarianteEndpoints();
```

- [ ] **Step 11: Migration e build**

```bash
dotnet ef migrations add AddVariantes --project FacShopAPI.csproj
dotnet build FacShopAPI.csproj
```
Esperado: migration criada, `Build succeeded, 0 errors`.

- [ ] **Step 12: Testes de domÃ­nio Variante**

Crie `tests/FacShopAPI.Tests/Unit/Domain/VarianteTests.cs`:

```csharp
using FluentAssertions;
using FacShopAPI.Catalogo.Domain;
using Xunit;

namespace FacShopAPI.Tests.Unit.Domain;

public class VarianteTests
{
    [Fact]
    public void Criar_DadosValidos_RetornaSucesso()
    {
        var result = Variante.Criar(1, "PROD-001", "P / Azul", 0.01m, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sku.Valor.Should().Be("PROD-001");
        result.Value.ProdutoId.Should().Be(1);
        result.Value.Ativa.Should().BeTrue();
    }

    [Theory]
    [InlineData("abc")]        // muito curto
    [InlineData("PRODUTO-SKU-MUITO-LONGO-DEMAIS")]  // muito longo
    [InlineData("sku_minusculo")]  // caractere invÃ¡lido
    public void Criar_SkuInvalido_RetornaFalha(string sku)
    {
        var result = Variante.Criar(1, sku, "DescriÃ§Ã£o", 10m, 5);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Criar_PrecoZero_RetornaFalha()
    {
        var result = Variante.Criar(1, "SKU-001", "DescriÃ§Ã£o", 0m, 5);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Criar_EstoqueNegativo_RetornaFalha()
    {
        var result = Variante.Criar(1, "SKU-001", "DescriÃ§Ã£o", 10m, -1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AtualizarPreco_PrecoValido_AtualizaComSucesso()
    {
        var variante = Variante.Criar(1, "SKU-001", "DescriÃ§Ã£o", 10m, 5).Value!;

        var result = variante.AtualizarPreco(20m);

        result.IsSuccess.Should().BeTrue();
        variante.PrecoAdicional.Value.Should().Be(20m);
    }

    [Fact]
    public void AtualizarEstoque_EstoqueValido_AtualizaComSucesso()
    {
        var variante = Variante.Criar(1, "SKU-001", "DescriÃ§Ã£o", 10m, 5).Value!;

        var result = variante.AtualizarEstoque(100);

        result.IsSuccess.Should().BeTrue();
        variante.Estoque.Value.Should().Be(100);
    }

    [Fact]
    public void Desativar_VarianteAtiva_DesativaComSucesso()
    {
        var variante = Variante.Criar(1, "SKU-001", "DescriÃ§Ã£o", 10m, 5).Value!;

        var result = variante.Desativar();

        result.IsSuccess.Should().BeTrue();
        variante.Ativa.Should().BeFalse();
    }
}
```

- [ ] **Step 13: Rodar todos os testes**

```bash
dotnet test tests/FacShopAPI.Tests/FacShopAPI.Tests.csproj -v minimal
```
Esperado: todos os testes passando.

- [ ] **Step 14: Commit Variante**

```bash
git add -A
git commit -m "feat: adicionar recurso Variante ao CatÃ¡logo

- Variante.cs com SKU (value object), PrecoAdicional, Estoque
- SKU.cs: ^[A-Z0-9\\-]+$, 6-20 chars, Ãºnico por produto
- VarianteService valida unicidade de SKU por produto
- 6 endpoints em /api/v1/catalogo/variantes
- Migration AddVariantes"
```

---

### Task 8: Atributo e MÃ­dia â€” CRUD Simples

**Files:**
- Create: `src/Catalogo/Catalogo.Domain/Atributo.cs`
- Create: `src/Catalogo/Catalogo.Domain/Midia.cs`
- Create: `src/Catalogo/Catalogo.Application/DTOs/Atributo/AtributoDTO.cs`
- Create: `src/Catalogo/Catalogo.Application/DTOs/Midia/MidiaDTO.cs`
- Create: `src/Catalogo/Catalogo.Application/Repositories/IAtributoRepository.cs`
- Create: `src/Catalogo/Catalogo.Application/Repositories/IMidiaRepository.cs`
- Create: `src/Catalogo/Catalogo.Application/Services/IAtributoService.cs`
- Create: `src/Catalogo/Catalogo.Application/Services/AtributoService.cs`
- Create: `src/Catalogo/Catalogo.Application/Services/IMidiaService.cs`
- Create: `src/Catalogo/Catalogo.Application/Services/MidiaService.cs`
- Create: `src/Catalogo/Catalogo.Infrastructure/Repositories/EfAtributoRepository.cs`
- Create: `src/Catalogo/Catalogo.Infrastructure/Repositories/EfMidiaRepository.cs`
- Create: `src/Catalogo/Catalogo.API/Endpoints/Atributos/AtributoEndpoints.cs`
- Create: `src/Catalogo/Catalogo.API/Endpoints/Midias/MidiaEndpoints.cs`
- Modify: `src/Catalogo/Catalogo.Application/Interfaces/ICatalogoContext.cs`
- Modify: `src/Shared/Data/AppDbContext.cs`
- Modify: `src/Catalogo/Catalogo.API/Extensions/CatalogoServiceExtensions.cs`
- Modify: `Program.cs`

> **Nota:** Atributo e MÃ­dia sÃ£o recursos mais simples â€” sem CQRS separado (repositÃ³rio Ãºnico, sem Dapper separado). O valor didÃ¡tico aqui Ã© mostrar que nem todo recurso do mesmo bounded context precisa de domÃ­nio rico.

- [ ] **Step 1: Criar Atributo.cs**

Crie `src/Catalogo/Catalogo.Domain/Atributo.cs`:

```csharp
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Domain;

public class Atributo
{
    private Atributo() { }

    public int Id { get; private set; }
    public int ProdutoId { get; private set; }
    public string Chave { get; private set; } = "";
    public string Valor { get; private set; } = "";
    public DateTime DataCriacao { get; private set; }

    public static Result<Atributo> Criar(int produtoId, string chave, string valor)
    {
        if (produtoId <= 0) return Result<Atributo>.Fail("ProdutoId invÃ¡lido.");
        if (string.IsNullOrWhiteSpace(chave) || chave.Length > 50)
            return Result<Atributo>.Fail("Chave deve ter entre 1 e 50 caracteres.");
        if (string.IsNullOrWhiteSpace(valor) || valor.Length > 200)
            return Result<Atributo>.Fail("Valor deve ter entre 1 e 200 caracteres.");

        return Result<Atributo>.Ok(new Atributo
        {
            ProdutoId = produtoId,
            Chave = chave.Trim(),
            Valor = valor.Trim(),
            DataCriacao = DateTime.UtcNow
        });
    }

    public Result Atualizar(string chave, string valor)
    {
        if (string.IsNullOrWhiteSpace(chave) || chave.Length > 50)
            return Result.Fail("Chave deve ter entre 1 e 50 caracteres.");
        if (string.IsNullOrWhiteSpace(valor) || valor.Length > 200)
            return Result.Fail("Valor deve ter entre 1 e 200 caracteres.");
        Chave = chave.Trim();
        Valor = valor.Trim();
        return Result.Ok();
    }
}
```

- [ ] **Step 2: Criar Midia.cs**

Crie `src/Catalogo/Catalogo.Domain/Midia.cs`:

```csharp
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Domain;

public enum TipoMidia { Imagem, Video, Documento }

public class Midia
{
    private Midia() { }

    public int Id { get; private set; }
    public int ProdutoId { get; private set; }
    public string Url { get; private set; } = "";
    public TipoMidia Tipo { get; private set; }
    public int Ordem { get; private set; }
    public DateTime DataCriacao { get; private set; }

    public static Result<Midia> Criar(int produtoId, string url, TipoMidia tipo, int ordem = 0)
    {
        if (produtoId <= 0) return Result<Midia>.Fail("ProdutoId invÃ¡lido.");
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return Result<Midia>.Fail("URL invÃ¡lida. Deve ser uma URL absoluta.");
        if (url.Length > 500) return Result<Midia>.Fail("URL nÃ£o pode exceder 500 caracteres.");
        if (ordem < 0) return Result<Midia>.Fail("Ordem nÃ£o pode ser negativa.");

        return Result<Midia>.Ok(new Midia
        {
            ProdutoId = produtoId, Url = url.Trim(),
            Tipo = tipo, Ordem = ordem,
            DataCriacao = DateTime.UtcNow
        });
    }

    public Result AtualizarOrdem(int novaOrdem)
    {
        if (novaOrdem < 0) return Result.Fail("Ordem nÃ£o pode ser negativa.");
        Ordem = novaOrdem;
        return Result.Ok();
    }
}
```

- [ ] **Step 3: Criar AtributoDTO.cs e MidiaDTO.cs**

Crie `src/Catalogo/Catalogo.Application/DTOs/Atributo/AtributoDTO.cs`:

```csharp
namespace FacShopAPI.Catalogo.Application.DTOs.Atributo;

public class CriarAtributoRequest
{
    public int ProdutoId { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}

public class AtualizarAtributoRequest
{
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}

public class AtributoResponse
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}
```

Crie `src/Catalogo/Catalogo.Application/DTOs/Midia/MidiaDTO.cs`:

```csharp
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.DTOs.Midia;

public class CriarMidiaRequest
{
    public int ProdutoId { get; set; }
    public string Url { get; set; } = string.Empty;
    public TipoMidia Tipo { get; set; }
    public int Ordem { get; set; }
}

public class AtualizarOrdemMidiaRequest
{
    public int Ordem { get; set; }
}

public class MidiaResponse
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string Url { get; set; } = string.Empty;
    public TipoMidia Tipo { get; set; }
    public int Ordem { get; set; }
    public DateTime DataCriacao { get; set; }
}
```

- [ ] **Step 4: Criar IAtributoRepository e IMidiaRepository**

Crie `src/Catalogo/Catalogo.Application/Repositories/IAtributoRepository.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Atributo;
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IAtributoRepository
{
    Task<List<AtributoResponse>> ListarPorProdutoAsync(int produtoId);
    Task<Atributo?> ObterPorIdAsync(int id);
    Task<Atributo> AdicionarAsync(Atributo atributo);
    Task RemoverAsync(int id);
    Task SaveChangesAsync();
}
```

Crie `src/Catalogo/Catalogo.Application/Repositories/IMidiaRepository.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Midia;
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IMidiaRepository
{
    Task<List<MidiaResponse>> ListarPorProdutoAsync(int produtoId);
    Task<Midia?> ObterPorIdAsync(int id);
    Task<Midia> AdicionarAsync(Midia midia);
    Task RemoverAsync(int id);
    Task SaveChangesAsync();
}
```

- [ ] **Step 5: Criar AtributoService.cs e MidiaService.cs**

Crie `src/Catalogo/Catalogo.Application/Services/IAtributoService.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Atributo;
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public interface IAtributoService
{
    Task<List<AtributoResponse>> ListarPorProdutoAsync(int produtoId);
    Task<Result<AtributoResponse>> CriarAsync(CriarAtributoRequest request);
    Task<Result<AtributoResponse>> AtualizarAsync(int id, AtualizarAtributoRequest request);
    Task<Result> RemoverAsync(int id);
}
```

Crie `src/Catalogo/Catalogo.Application/Services/AtributoService.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Atributo;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Domain;
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public class AtributoService : IAtributoService
{
    private readonly IAtributoRepository _repo;

    public AtributoService(IAtributoRepository repo) => _repo = repo;

    public Task<List<AtributoResponse>> ListarPorProdutoAsync(int produtoId) =>
        _repo.ListarPorProdutoAsync(produtoId);

    public async Task<Result<AtributoResponse>> CriarAsync(CriarAtributoRequest request)
    {
        var result = Atributo.Criar(request.ProdutoId, request.Chave, request.Valor);
        if (!result.IsSuccess) return Result<AtributoResponse>.Fail(result.Error!);
        var atributo = await _repo.AdicionarAsync(result.Value!);
        return Result<AtributoResponse>.Ok(MapToResponse(atributo));
    }

    public async Task<Result<AtributoResponse>> AtualizarAsync(int id, AtualizarAtributoRequest request)
    {
        var atributo = await _repo.ObterPorIdAsync(id);
        if (atributo is null) return Result<AtributoResponse>.Fail("Atributo nÃ£o encontrado.");
        var r = atributo.Atualizar(request.Chave, request.Valor);
        if (!r.IsSuccess) return Result<AtributoResponse>.Fail(r.Error!);
        await _repo.SaveChangesAsync();
        return Result<AtributoResponse>.Ok(MapToResponse(atributo));
    }

    public async Task<Result> RemoverAsync(int id)
    {
        var atributo = await _repo.ObterPorIdAsync(id);
        if (atributo is null) return Result.Fail("Atributo nÃ£o encontrado.");
        await _repo.RemoverAsync(id);
        return Result.Ok();
    }

    private static AtributoResponse MapToResponse(Atributo a) =>
        new() { Id = a.Id, ProdutoId = a.ProdutoId, Chave = a.Chave, Valor = a.Valor, DataCriacao = a.DataCriacao };
}
```

Crie `src/Catalogo/Catalogo.Application/Services/IMidiaService.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Midia;
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public interface IMidiaService
{
    Task<List<MidiaResponse>> ListarPorProdutoAsync(int produtoId);
    Task<Result<MidiaResponse>> CriarAsync(CriarMidiaRequest request);
    Task<Result<MidiaResponse>> AtualizarOrdemAsync(int id, AtualizarOrdemMidiaRequest request);
    Task<Result> RemoverAsync(int id);
}
```

Crie `src/Catalogo/Catalogo.Application/Services/MidiaService.cs`:

```csharp
using FacShopAPI.Catalogo.Application.DTOs.Midia;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Domain;
using FacShopAPI.Catalogo.Domain.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public class MidiaService : IMidiaService
{
    private readonly IMidiaRepository _repo;

    public MidiaService(IMidiaRepository repo) => _repo = repo;

    public Task<List<MidiaResponse>> ListarPorProdutoAsync(int produtoId) =>
        _repo.ListarPorProdutoAsync(produtoId);

    public async Task<Result<MidiaResponse>> CriarAsync(CriarMidiaRequest request)
    {
        var result = Midia.Criar(request.ProdutoId, request.Url, request.Tipo, request.Ordem);
        if (!result.IsSuccess) return Result<MidiaResponse>.Fail(result.Error!);
        var midia = await _repo.AdicionarAsync(result.Value!);
        return Result<MidiaResponse>.Ok(MapToResponse(midia));
    }

    public async Task<Result<MidiaResponse>> AtualizarOrdemAsync(int id, AtualizarOrdemMidiaRequest request)
    {
        var midia = await _repo.ObterPorIdAsync(id);
        if (midia is null) return Result<MidiaResponse>.Fail("MÃ­dia nÃ£o encontrada.");
        var r = midia.AtualizarOrdem(request.Ordem);
        if (!r.IsSuccess) return Result<MidiaResponse>.Fail(r.Error!);
        await _repo.SaveChangesAsync();
        return Result<MidiaResponse>.Ok(MapToResponse(midia));
    }

    public async Task<Result> RemoverAsync(int id)
    {
        var midia = await _repo.ObterPorIdAsync(id);
        if (midia is null) return Result.Fail("MÃ­dia nÃ£o encontrada.");
        await _repo.RemoverAsync(id);
        return Result.Ok();
    }

    private static MidiaResponse MapToResponse(Midia m) =>
        new() { Id = m.Id, ProdutoId = m.ProdutoId, Url = m.Url, Tipo = m.Tipo, Ordem = m.Ordem, DataCriacao = m.DataCriacao };
}
```

- [ ] **Step 6: Atualizar ICatalogoContext**

```csharp
public interface ICatalogoContext
{
    IQueryable<Produto> Produtos { get; }
    IQueryable<Categoria> Categorias { get; }
    IQueryable<Variante> Variantes { get; }
    IQueryable<Atributo> Atributos { get; }
    IQueryable<Midia> Midias { get; }
    void AddProduto(Produto produto);
    void AddCategoria(Categoria categoria);
    void AddVariante(Variante variante);
    void AddAtributo(Atributo atributo);
    void AddMidia(Midia midia);
    void Remove<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 7: Atualizar AppDbContext**

```csharp
// DbSets
public DbSet<Atributo> Atributos => Set<Atributo>();
public DbSet<Midia> Midias => Set<Midia>();

// ICatalogoContext
IQueryable<Atributo> ICatalogoContext.Atributos => Set<Atributo>();
IQueryable<Midia> ICatalogoContext.Midias => Set<Midia>();
public void AddAtributo(Atributo atributo) => this.Add(atributo);
public void AddMidia(Midia midia) => this.Add(midia);
public void Remove<T>(T entity) where T : class => base.Remove(entity!);
```

No `OnModelCreating`:

```csharp
modelBuilder.Entity<Atributo>(entity =>
{
    entity.HasKey(a => a.Id);
    entity.Property(a => a.ProdutoId).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
    entity.Property(a => a.Chave).IsRequired().HasMaxLength(50).UsePropertyAccessMode(PropertyAccessMode.Property);
    entity.Property(a => a.Valor).IsRequired().HasMaxLength(200).UsePropertyAccessMode(PropertyAccessMode.Property);
    entity.Property(a => a.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
});

modelBuilder.Entity<Midia>(entity =>
{
    entity.HasKey(m => m.Id);
    entity.Property(m => m.ProdutoId).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
    entity.Property(m => m.Url).IsRequired().HasMaxLength(500).UsePropertyAccessMode(PropertyAccessMode.Property);
    entity.Property(m => m.Tipo).HasConversion<string>().UsePropertyAccessMode(PropertyAccessMode.Property);
    entity.Property(m => m.Ordem).HasDefaultValue(0).UsePropertyAccessMode(PropertyAccessMode.Property);
    entity.Property(m => m.DataCriacao).UsePropertyAccessMode(PropertyAccessMode.Property);
});
```

- [ ] **Step 8: Criar EfAtributoRepository.cs e EfMidiaRepository.cs**

Crie `src/Catalogo/Catalogo.Infrastructure/Repositories/EfAtributoRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Catalogo.Application.DTOs.Atributo;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Infrastructure.Repositories;

public class EfAtributoRepository(ICatalogoContext context) : IAtributoRepository
{
    public async Task<List<AtributoResponse>> ListarPorProdutoAsync(int produtoId)
    {
        var atributos = await context.Atributos
            .Where(a => a.ProdutoId == produtoId)
            .OrderBy(a => a.Chave)
            .ToListAsync();

        return atributos.Select(a => new AtributoResponse
        {
            Id = a.Id, ProdutoId = a.ProdutoId,
            Chave = a.Chave, Valor = a.Valor, DataCriacao = a.DataCriacao
        }).ToList();
    }

    public Task<Atributo?> ObterPorIdAsync(int id) =>
        context.Atributos.FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Atributo> AdicionarAsync(Atributo atributo)
    {
        context.AddAtributo(atributo);
        await context.SaveChangesAsync();
        return atributo;
    }

    public async Task RemoverAsync(int id)
    {
        var atributo = await context.Atributos.FirstOrDefaultAsync(a => a.Id == id);
        if (atributo is not null)
        {
            context.Remove(atributo);
            await context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();
}
```

Crie `src/Catalogo/Catalogo.Infrastructure/Repositories/EfMidiaRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using FacShopAPI.Catalogo.Application.DTOs.Midia;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Infrastructure.Repositories;

public class EfMidiaRepository(ICatalogoContext context) : IMidiaRepository
{
    public async Task<List<MidiaResponse>> ListarPorProdutoAsync(int produtoId)
    {
        var midias = await context.Midias
            .Where(m => m.ProdutoId == produtoId)
            .OrderBy(m => m.Ordem)
            .ToListAsync();

        return midias.Select(m => new MidiaResponse
        {
            Id = m.Id, ProdutoId = m.ProdutoId, Url = m.Url,
            Tipo = m.Tipo, Ordem = m.Ordem, DataCriacao = m.DataCriacao
        }).ToList();
    }

    public Task<Midia?> ObterPorIdAsync(int id) =>
        context.Midias.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<Midia> AdicionarAsync(Midia midia)
    {
        context.AddMidia(midia);
        await context.SaveChangesAsync();
        return midia;
    }

    public async Task RemoverAsync(int id)
    {
        var midia = await context.Midias.FirstOrDefaultAsync(m => m.Id == id);
        if (midia is not null)
        {
            context.Remove(midia);
            await context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();
}
```

- [ ] **Step 9: Criar AtributoEndpoints.cs e MidiaEndpoints.cs**

Crie `src/Catalogo/Catalogo.API/Endpoints/Atributos/AtributoEndpoints.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FacShopAPI.Catalogo.API.DTOs;
using FacShopAPI.Catalogo.Application.DTOs.Atributo;
using FacShopAPI.Catalogo.Application.Services;

namespace FacShopAPI.Catalogo.API.Endpoints.Atributos;

public static class AtributoEndpoints
{
    public static void MapAtributoEndpoints(this RouteGroupBuilder catalogoGroup)
    {
        var group = catalogoGroup.MapGroup("/atributos").WithTags("CatÃ¡logo - Atributos");

        group.MapGet("/", Listar).WithName("ListarAtributos")
            .Produces<List<AtributoResponse>>(StatusCodes.Status200OK).AllowAnonymous();

        group.MapPost("/", Criar).WithName("CriarAtributo")
            .Accepts<CriarAtributoRequest>("application/json")
            .Produces<AtributoResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        group.MapPut("/{id:int}", Atualizar).WithName("AtualizarAtributo")
            .Accepts<AtualizarAtributoRequest>("application/json")
            .Produces<AtributoResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapDelete("/{id:int}", Remover).WithName("RemoverAtributo")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    private static async Task<IResult> Listar(IAtributoService service, int produtoId) =>
        Results.Ok(await service.ListarPorProdutoAsync(produtoId));

    private static async Task<IResult> Criar(CriarAtributoRequest request, IAtributoService service)
    {
        var result = await service.CriarAsync(request);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Erro", Detail = result.Error! });
        return Results.Created($"/api/v1/catalogo/atributos/{result.Value!.Id}", result.Value);
    }

    private static async Task<IResult> Atualizar(int id, AtualizarAtributoRequest request, IAtributoService service)
    {
        var result = await service.AtualizarAsync(id, request);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "NÃ£o encontrado", Detail = result.Error! });
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> Remover(int id, IAtributoService service)
    {
        var result = await service.RemoverAsync(id);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "NÃ£o encontrado", Detail = result.Error! });
        return Results.NoContent();
    }
}
```

Crie `src/Catalogo/Catalogo.API/Endpoints/Midias/MidiaEndpoints.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FacShopAPI.Catalogo.API.DTOs;
using FacShopAPI.Catalogo.Application.DTOs.Midia;
using FacShopAPI.Catalogo.Application.Services;

namespace FacShopAPI.Catalogo.API.Endpoints.Midias;

public static class MidiaEndpoints
{
    public static void MapMidiaEndpoints(this RouteGroupBuilder catalogoGroup)
    {
        var group = catalogoGroup.MapGroup("/midias").WithTags("CatÃ¡logo - MÃ­dias");

        group.MapGet("/", Listar).WithName("ListarMidias")
            .Produces<List<MidiaResponse>>(StatusCodes.Status200OK).AllowAnonymous();

        group.MapPost("/", Criar).WithName("CriarMidia")
            .Accepts<CriarMidiaRequest>("application/json")
            .Produces<MidiaResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization();

        group.MapPatch("/{id:int}/ordem", AtualizarOrdem).WithName("AtualizarOrdemMidia")
            .Accepts<AtualizarOrdemMidiaRequest>("application/json")
            .Produces<MidiaResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapDelete("/{id:int}", Remover).WithName("RemoverMidia")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }

    private static async Task<IResult> Listar(IMidiaService service, int produtoId) =>
        Results.Ok(await service.ListarPorProdutoAsync(produtoId));

    private static async Task<IResult> Criar(CriarMidiaRequest request, IMidiaService service)
    {
        var result = await service.CriarAsync(request);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Erro", Detail = result.Error! });
        return Results.Created($"/api/v1/catalogo/midias/{result.Value!.Id}", result.Value);
    }

    private static async Task<IResult> AtualizarOrdem(int id, AtualizarOrdemMidiaRequest request, IMidiaService service)
    {
        var result = await service.AtualizarOrdemAsync(id, request);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "NÃ£o encontrado", Detail = result.Error! });
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> Remover(int id, IMidiaService service)
    {
        var result = await service.RemoverAsync(id);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "NÃ£o encontrado", Detail = result.Error! });
        return Results.NoContent();
    }
}
```

- [ ] **Step 10: Atualizar CatalogoServiceExtensions com Atributo e MÃ­dia**

```csharp
services.AddScoped<IAtributoService, AtributoService>();
services.AddScoped<IAtributoRepository, EfAtributoRepository>();

services.AddScoped<IMidiaService, MidiaService>();
services.AddScoped<IMidiaRepository, EfMidiaRepository>();
```

- [ ] **Step 11: Atualizar Program.cs**

```csharp
using FacShopAPI.Catalogo.API.Endpoints.Atributos;
using FacShopAPI.Catalogo.API.Endpoints.Midias;
// ...
catalogo.MapAtributoEndpoints();
catalogo.MapMidiaEndpoints();
```

- [ ] **Step 12: Migration e build**

```bash
dotnet ef migrations add AddAtributosEMidias --project FacShopAPI.csproj
dotnet build FacShopAPI.csproj
dotnet test tests/FacShopAPI.Tests/FacShopAPI.Tests.csproj -v minimal
```
Esperado: migration criada, build OK, todos os testes passando.

- [ ] **Step 13: Commit Atributo + MÃ­dia**

```bash
git add -A
git commit -m "feat: adicionar Atributo e Midia ao CatÃ¡logo (CRUD simples)

- Atributo: chave-valor por produto, CRUD completo
- Midia: URL externa com tipo e ordem por produto, CRUD completo
- Demonstra que no mesmo bounded context coexistem recursos
  de complexidade diferente (sem domÃ­nio rico)
- Migration AddAtributosEMidias"
```

---

### Task 9: Atualizar DbSeeder com dados de seed para novos recursos

**Files:**
- Modify: `src/Catalogo/Catalogo.Infrastructure/Data/DbSeeder.cs`
- Modify: `tests/FacShopAPI.Tests/Integration/ApiFactory.cs`

- [ ] **Step 1: Atualizar DbSeeder**

O seeder atual sÃ³ popula Produtos. Adicione seed de Categorias para os testes terem IDs reservados consistentes.

Em `src/Catalogo/Catalogo.Infrastructure/Data/DbSeeder.cs`:

```csharp
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Domain;

namespace FacShopAPI.Catalogo.Infrastructure.Data;

public static class DbSeeder
{
    public static void Seed(ICatalogoContext context)
    {
        SeedProdutos(context);
        SeedCategorias(context);
    }

    private static void SeedProdutos(ICatalogoContext context)
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

    private static void SeedCategorias(ICatalogoContext context)
    {
        if (context.Categorias.Any()) return;

        // IDs 1-5 reservados: testes de Categoria criam a partir de ID 6
        var raiz = new[]
        {
            Categoria.Criar("EletrÃ´nicos").Value!,
            Categoria.Criar("Livros").Value!,
            Categoria.Criar("Roupas").Value!,
            Categoria.Criar("Alimentos").Value!,
            Categoria.Criar("Outros").Value!
        };

        foreach (var c in raiz) context.AddCategoria(c);
        context.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
```

- [ ] **Step 2: Rodar testes completos**

```bash
dotnet test tests/FacShopAPI.Tests/FacShopAPI.Tests.csproj -v minimal
```
Esperado: todos os testes passando.

- [ ] **Step 3: Commit final da Fase 2**

```bash
git add -A
git commit -m "feat: seed de Categorias no DbSeeder â€” IDs 1-5 reservados

Testes de Categoria criam a partir do ID 6."
```
