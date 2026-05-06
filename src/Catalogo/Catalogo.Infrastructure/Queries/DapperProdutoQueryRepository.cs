using System.Data;
using System.Data.Common;
using System.Text;
using Dapper;
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FacShopAPI.Catalogo.Infrastructure.Queries;

public class DapperProdutoQueryRepository : IProdutoQueryRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<DapperProdutoQueryRepository> _logger;

    public DapperProdutoQueryRepository(ICatalogoContext context, ILogger<DapperProdutoQueryRepository> logger)
    {
        _dbContext = context as DbContext
            ?? throw new InvalidOperationException(
                "ICatalogoContext precisa ser uma implementação de DbContext para suportar Dapper.");
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
        Id = row.Id,
        Nome = row.Nome,
        Descricao = row.Descricao,
        Preco = row.Preco,
        Categoria = row.Categoria,
        Estoque = row.Estoque,
        Ativo = row.Ativo,
        ContatoEmail = row.ContatoEmail,
        DataCriacao = row.DataCriacao,
        DataAtualizacao = row.DataAtualizacao
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
