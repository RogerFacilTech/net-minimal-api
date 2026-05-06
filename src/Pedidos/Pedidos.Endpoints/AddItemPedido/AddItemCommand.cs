using ProdutosAPI.Shared.Common;
using ProdutosAPI.Pedidos.Common;
using ProdutosAPI.Pedidos.Repositories;

namespace ProdutosAPI.Pedidos.AddItemPedido;

public record AddItemCommand(int PedidoId, int ProdutoId, int Quantidade);

public class AddItemHandler(IPedidoCommandRepository repository)
{
    public async Task<Result<PedidoResponse>> HandleAsync(
        AddItemCommand cmd, CancellationToken ct = default)
    {
        var pedido = await repository.ObterPorIdAsync(cmd.PedidoId, ct);

        if (pedido is null)
            return Result<PedidoResponse>.Fail("Pedido não encontrado.");

        var produto = await repository.ObterProdutoParaItemAsync(cmd.ProdutoId, ct);
        if (produto is null)
            return Result<PedidoResponse>.Fail($"Produto {cmd.ProdutoId} não encontrado.");

        var resultado = pedido.AdicionarItem(produto, cmd.Quantidade);
        if (!resultado.IsSuccess)
            return Result<PedidoResponse>.Fail(resultado.Error!);

        await repository.SaveChangesAsync(ct);
        return Result<PedidoResponse>.Ok(PedidoResponse.From(pedido));
    }
}
