using System.Data;
using System.Data.Common;
using Dapper;
using FacShopAPI.Catalogo.Application.DTOs.Variante;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;
using Microsoft.EntityFrameworkCore;

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
        Id = row.Id,
        ProdutoId = row.ProdutoId,
        Sku = row.Sku,
        Descricao = row.Descricao,
        PrecoAdicional = row.PrecoAdicional,
        Estoque = row.Estoque,
        Ativa = row.Ativa,
        DataCriacao = row.DataCriacao,
        DataAtualizacao = row.DataAtualizacao
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
