using ProdutosAPI.Shared.Common;
using ProdutosAPI.Pedidos.Common;
using ProdutosAPI.Pedidos.Repositories;

namespace ProdutosAPI.Pedidos.GetPedido;

public record GetPedidoQuery(int Id);

public class GetPedidoHandler(IPedidoQueryRepository repository)
{
    public async Task<Result<PedidoResponse>> HandleAsync(
        GetPedidoQuery query, CancellationToken ct = default)
    {
        var pedido = await repository.ObterPorIdAsync(query.Id, ct);

        if (pedido is null)
            return Result<PedidoResponse>.Fail("Pedido não encontrado.");

        return Result<PedidoResponse>.Ok(pedido);
    }
}
