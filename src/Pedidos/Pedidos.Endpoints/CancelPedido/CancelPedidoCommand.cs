using ProdutosAPI.Shared.Common;
using ProdutosAPI.Pedidos.Common;
using ProdutosAPI.Pedidos.Repositories;

namespace ProdutosAPI.Pedidos.CancelPedido;

public record CancelPedidoRequest(string Motivo);

public record CancelPedidoCommand(int PedidoId, string Motivo);

public class CancelPedidoHandler(IPedidoCommandRepository repository)
{
    public async Task<Result<PedidoResponse>> HandleAsync(
        CancelPedidoCommand cmd, CancellationToken ct = default)
    {
        var pedido = await repository.ObterPorIdAsync(cmd.PedidoId, ct);

        if (pedido is null)
            return Result<PedidoResponse>.Fail("Pedido não encontrado.");

        var resultado = pedido.Cancelar(cmd.Motivo);
        if (!resultado.IsSuccess)
            return Result<PedidoResponse>.Fail(resultado.Error!);

        await repository.SaveChangesAsync(ct);
        return Result<PedidoResponse>.Ok(PedidoResponse.From(pedido));
    }
}
