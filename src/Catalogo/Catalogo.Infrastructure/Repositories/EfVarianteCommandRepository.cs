using Catalogo.Domain;
using Microsoft.EntityFrameworkCore;
using ProdutosAPI.Catalogo.Application.Interfaces;
using ProdutosAPI.Catalogo.Application.Repositories;

namespace ProdutosAPI.Catalogo.Infrastructure.Repositories;

public class EfVarianteCommandRepository(ICatalogoContext context) : IVarianteCommandRepository
{
    public Task<Variante?> ObterPorIdAsync(int id) =>
        context.Variantes.FirstOrDefaultAsync(v => v.Id == id && v.Ativa);

    public async Task<Variante> AdicionarAsync(Variante variante)
    {
        context.AddVariante(variante);
        await context.SaveChangesAsync();
        return variante;
    }

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();

    public async Task<bool> SkuExisteParaProdutoAsync(int produtoId, string sku) =>
        await context.Variantes.AnyAsync(v => v.ProdutoId == produtoId && v.Sku.Valor == sku && v.Ativa);
}
