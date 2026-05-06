using System.Data;
using System.Data.Common;
using Dapper;
using FacShopAPI.Catalogo.Application.DTOs.Categoria;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;
using Microsoft.EntityFrameworkCore;

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
                    Id = r.Id,
                    Nome = r.Nome,
                    Slug = r.Slug,
                    CategoriaPaiId = r.CategoriaPaiId,
                    Ativa = r.Ativa,
                    DataCriacao = r.DataCriacao,
                    Subcategorias = rows
                        .Where(s => s.CategoriaPaiId == r.Id)
                        .Select(s => new CategoriaResponse
                        {
                            Id = s.Id,
                            Nome = s.Nome,
                            Slug = s.Slug,
                            CategoriaPaiId = s.CategoriaPaiId,
                            Ativa = s.Ativa,
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
                Id = row.Id,
                Nome = row.Nome,
                Slug = row.Slug,
                CategoriaPaiId = row.CategoriaPaiId,
                Ativa = row.Ativa,
                DataCriacao = row.DataCriacao,
                Subcategorias = subs.Select(s => new CategoriaResponse
                {
                    Id = s.Id,
                    Nome = s.Nome,
                    Slug = s.Slug,
                    CategoriaPaiId = s.CategoriaPaiId,
                    Ativa = s.Ativa,
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
