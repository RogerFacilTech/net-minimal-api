using FacShopAPI.Pedidos.Common;
using FacShopAPI.Pedidos.Domain;
using FacShopAPI.Pedidos.Repositories;
using FacShopAPI.Shared.Kernel;
using FacShopAPI.Shared.Web;

namespace FacShopAPI.Pedidos.ListPedidos;

public record ListPedidosQuery(int Page = 1, int PageSize = 20, string? Status = null);

public record ListPedidosResponse(List<PedidoResponse> Data, int Total, int Page, int PageSize);

public class ListPedidosHandler(IPedidoQueryRepository repository)
{
    public async Task<Result<ListPedidosResponse>> HandleAsync(
        ListPedidosQuery query, CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);

        StatusPedido? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<StatusPedido>(query.Status, true, out var parsed))
        {
            statusEnum = parsed;
        }

        var (data, total) = await repository.ListarAsync(page, pageSize, statusEnum, ct);

        return Result<ListPedidosResponse>.Ok(
            new ListPedidosResponse(data.ToList(), total, page, pageSize));
    }
}
