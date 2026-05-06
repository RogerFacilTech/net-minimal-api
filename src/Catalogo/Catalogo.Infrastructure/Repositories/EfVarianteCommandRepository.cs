using Catalogo.Domain;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FacShopAPI.Catalogo.Infrastructure.Repositories;

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
