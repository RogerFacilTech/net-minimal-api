using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.EntityFrameworkCore;
using ProdutosAPI.Pedidos.Common;
using ProdutosAPI.Pedidos.Domain;
using ProdutosAPI.Pedidos.Repositories;
using ProdutosAPI.Shared.Data;

namespace ProdutosAPI.Pedidos.Infrastructure;

public class PedidoQueryRepository(AppDbContext db) : IPedidoQueryRepository
{
    public Task<PedidoResponse?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        WithConnectionAsync(async connection =>
        {
            const string pedidoSql = @"
SELECT Id, Status, Total, CriadoEm, ConfirmadoEm, CanceladoEm, MotivoCancelamento
FROM Pedidos WHERE Id = @Id;";

            const string itensSql = @"
SELECT ProdutoId, NomeProduto, PrecoUnitario, Quantidade
FROM PedidoItens WHERE PedidoId = @Id;";

            var pedidoRow = await connection.QuerySingleOrDefaultAsync<PedidoRow>(
                new CommandDefinition(pedidoSql, new { Id = id }, cancellationToken: ct));

            if (pedidoRow is null)
                return null;

            var itensRows = await connection.QueryAsync<PedidoItemRow>(
                new CommandDefinition(itensSql, new { Id = id }, cancellationToken: ct));

            var itens = itensRows
                .Select(i => new PedidoItemResponse(
                    i.ProdutoId,
                    i.NomeProduto,
                    i.PrecoUnitario,
                    i.Quantidade,
                    i.PrecoUnitario * i.Quantidade))
                .ToList();

            return new PedidoResponse(
                pedidoRow.Id,
                pedidoRow.Status,
                pedidoRow.Total,
                pedidoRow.CriadoEm,
                pedidoRow.ConfirmadoEm,
                pedidoRow.CanceladoEm,
                pedidoRow.MotivoCancelamento,
                itens);
        });

    public Task<(IReadOnlyList<PedidoResponse> Items, int Total)> ListarAsync(
        int page, int pageSize, StatusPedido? status = null, CancellationToken ct = default) =>
        WithConnectionAsync(async connection =>
        {
            var hasStatus = status.HasValue;
            var statusStr = status?.ToString();

            var countSql = hasStatus
                ? "SELECT COUNT(1) FROM Pedidos WHERE Status = @Status;"
                : "SELECT COUNT(1) FROM Pedidos;";

            var itemsSql = hasStatus
                ? @"SELECT Id, Status, Total, CriadoEm, ConfirmadoEm, CanceladoEm, MotivoCancelamento
FROM Pedidos WHERE Status = @Status
ORDER BY CriadoEm DESC
LIMIT @Limit OFFSET @Offset;"
                : @"SELECT Id, Status, Total, CriadoEm, ConfirmadoEm, CanceladoEm, MotivoCancelamento
FROM Pedidos
ORDER BY CriadoEm DESC
LIMIT @Limit OFFSET @Offset;";

            var parameters = new DynamicParameters();
            if (hasStatus) parameters.Add("Status", statusStr);
            parameters.Add("Limit", pageSize);
            parameters.Add("Offset", (page - 1) * pageSize);

            var total = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, hasStatus ? new { Status = statusStr } : null, cancellationToken: ct));

            var pedidoRows = await connection.QueryAsync<PedidoRow>(
                new CommandDefinition(itemsSql, parameters, cancellationToken: ct));

            var pedidoList = pedidoRows.ToList();

            if (pedidoList.Count == 0)
                return ((IReadOnlyList<PedidoResponse>)new List<PedidoResponse>(), total);

            var ids = pedidoList.Select(p => p.Id).ToList();

            const string itensSql = @"
SELECT ProdutoId, NomeProduto, PrecoUnitario, Quantidade, PedidoId
FROM PedidoItens WHERE PedidoId IN @Ids;";

            var todosItens = await connection.QueryAsync<PedidoItemRowWithPedidoId>(
                new CommandDefinition(itensSql, new { Ids = ids }, cancellationToken: ct));

            var itensPorPedido = todosItens
                .GroupBy(i => i.PedidoId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = pedidoList.Select(p =>
            {
                var itens = itensPorPedido.TryGetValue(p.Id, out var rows)
                    ? rows.Select(i => new PedidoItemResponse(
                        i.ProdutoId,
                        i.NomeProduto,
                        i.PrecoUnitario,
                        i.Quantidade,
                        i.PrecoUnitario * i.Quantidade)).ToList()
                    : new List<PedidoItemResponse>();

                return new PedidoResponse(
                    p.Id,
                    p.Status,
                    p.Total,
                    p.CriadoEm,
                    p.ConfirmadoEm,
                    p.CanceladoEm,
                    p.MotivoCancelamento,
                    itens);
            }).ToList();

            return ((IReadOnlyList<PedidoResponse>)result, total);
        });

    private async Task<T> WithConnectionAsync<T>(Func<DbConnection, Task<T>> action)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync();
        try { return await action(connection); }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private sealed class PedidoRow
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? ConfirmadoEm { get; set; }
        public DateTime? CanceladoEm { get; set; }
        public string? MotivoCancelamento { get; set; }
    }

    private sealed class PedidoItemRow
    {
        public int ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public int Quantidade { get; set; }
    }

    private sealed class PedidoItemRowWithPedidoId
    {
        public int PedidoId { get; set; }
        public int ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }
        public int Quantidade { get; set; }
    }
}
