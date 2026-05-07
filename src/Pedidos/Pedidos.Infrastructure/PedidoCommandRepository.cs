using FacShopAPI.Pedidos.Data;
using FacShopAPI.Pedidos.Domain;
using FacShopAPI.Pedidos.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FacShopAPI.Pedidos.Infrastructure;

public class PedidoCommandRepository(PedidosDbContext db, ICatalogoApiClient catalogoApiClient) : IPedidoCommandRepository
{

    public Task<Pedido?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<ProdutoSnapshot?> ObterProdutoParaItemAsync(int produtoId, CancellationToken ct = default) =>
        catalogoApiClient.ObterProdutoSnapshotAsync(produtoId, ct);

    public Task AdicionarAsync(Pedido pedido, CancellationToken ct = default)
    {
        db.Pedidos.Add(pedido);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
