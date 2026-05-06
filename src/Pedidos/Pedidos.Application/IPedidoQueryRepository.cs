using ProdutosAPI.Pedidos.Common;
using ProdutosAPI.Pedidos.Domain;

namespace ProdutosAPI.Pedidos.Repositories;

public interface IPedidoQueryRepository
{
    Task<PedidoResponse?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<(IReadOnlyList<PedidoResponse> Items, int Total)> ListarAsync(
        int page, int pageSize, StatusPedido? status = null, CancellationToken ct = default);
}
