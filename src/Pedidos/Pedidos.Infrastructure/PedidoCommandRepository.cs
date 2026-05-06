using Microsoft.EntityFrameworkCore;
using ProdutosAPI.Pedidos.Domain;
using ProdutosAPI.Pedidos.Repositories;
using ProdutosAPI.Shared.Data;

namespace ProdutosAPI.Pedidos.Infrastructure;

public class PedidoCommandRepository(AppDbContext db) : IPedidoCommandRepository
{

    public Task<Pedido?> ObterPorIdAsync(int id, CancellationToken ct = default) =>
        db.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<ProdutoSnapshot?> ObterProdutoParaItemAsync(int produtoId, CancellationToken ct = default)
    {
        var produto = await db.Produtos
            .Where(p => p.Id == produtoId)
            .Select(p => new { p.Id, p.Nome, p.Preco })
            .FirstOrDefaultAsync(ct);
        return produto is null
            ? null
            : new ProdutoSnapshot(produto.Id, produto.Nome, produto.Preco.Value);
    }

    public Task AdicionarAsync(Pedido pedido, CancellationToken ct = default)
    {
        db.Pedidos.Add(pedido);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
