using ProdutosAPI.Shared.Common;
using ProdutosAPI.Pedidos.Common;
using ProdutosAPI.Pedidos.Domain;
using ProdutosAPI.Pedidos.Repositories;

namespace ProdutosAPI.Pedidos.CreatePedido;

public record CreatePedidoCommand(List<CreatePedidoItemDto> Itens);
public record CreatePedidoItemDto(int ProdutoId, int Quantidade);

public class CreatePedidoHandler(IPedidoCommandRepository repository)
{
    public async Task<Result<PedidoResponse>> HandleAsync(
        CreatePedidoCommand cmd, CancellationToken ct = default)
    {
        var pedido = Pedido.Criar();

        foreach (var itemDto in cmd.Itens)
        {
            var produto = await repository.ObterProdutoParaItemAsync(itemDto.ProdutoId, ct);
            if (produto is null)
                return Result<PedidoResponse>.Fail($"Produto {itemDto.ProdutoId} não encontrado.");

            var resultado = pedido.AdicionarItem(produto, itemDto.Quantidade);
            if (!resultado.IsSuccess)
                return Result<PedidoResponse>.Fail(resultado.Error!);
        }

        await repository.AdicionarAsync(pedido, ct);
        await repository.SaveChangesAsync(ct);

        return Result<PedidoResponse>.Ok(PedidoResponse.From(pedido));
    }
}
