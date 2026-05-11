namespace FacShopAPI.Pedidos.Domain;

public interface IPedidoCommandRepository
{
    /// <summary>Carrega pedido com itens rastreado pelo EF para posterior mutação.</summary>
    Task<Pedido?> ObterPorIdAsync(int id, CancellationToken ct = default);
    /// <summary>Carrega snapshot do produto para inclusão no pedido.</summary>
    Task<ProdutoSnapshot?> ObterProdutoParaItemAsync(int produtoId, CancellationToken ct = default);
    Task AdicionarAsync(Pedido pedido, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
