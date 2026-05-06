using ProdutosAPI.Pedidos.Domain;

namespace ProdutosAPI.Pedidos.Common;

public record PedidoResponse(
    int Id,
    string Status,
    decimal Total,
    DateTime CriadoEm,
    DateTime? ConfirmadoEm,
    DateTime? CanceladoEm,
    string? MotivoCancelamento,
    List<PedidoItemResponse> Itens)
{
    public static PedidoResponse From(Pedido pedido) => new(
        pedido.Id,
        pedido.Status.ToString(),
        pedido.Total,
        pedido.CriadoEm,
        pedido.ConfirmadoEm,
        pedido.CanceladoEm,
        pedido.MotivoCancelamento,
        pedido.Itens.Select(PedidoItemResponse.From).ToList()
    );
}

public record PedidoItemResponse(
    int ProdutoId,
    string NomeProduto,
    decimal PrecoUnitario,
    int Quantidade,
    decimal Subtotal)
{
    public static PedidoItemResponse From(PedidoItem item) => new(
        item.ProdutoId,
        item.NomeProduto,
        item.PrecoUnitario,
        item.Quantidade,
        item.Subtotal
    );
}
