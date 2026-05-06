using FacShopAPI.Shared.Kernel;

namespace FacShopAPI.Pedidos.Domain;

public class PedidoItem
{
    public int Id { get; private set; }
    public int PedidoId { get; private set; }
    public int ProdutoId { get; private set; }
    public string NomeProduto { get; private set; } = "";
    public decimal PrecoUnitario { get; private set; }
    public int Quantidade { get; private set; }
    public decimal Subtotal => PrecoUnitario * Quantidade;

    private PedidoItem() { }

    internal static PedidoItem Criar(ProdutoSnapshot produto, int quantidade) => new()
    {
        ProdutoId = produto.Id,
        NomeProduto = produto.Nome,
        PrecoUnitario = produto.Preco,
        Quantidade = quantidade
    };

    internal void IncrementarQuantidade(int adicional)
    {
        if (adicional <= 0)
            throw new InvalidOperationException("Quantidade adicional deve ser positiva.");
        if (Quantidade + adicional > 999)
            throw new InvalidOperationException("Quantidade máxima por item é 999.");
        Quantidade += adicional;
    }
}
