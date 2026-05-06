using Catalogo.Domain;
using FacShopAPI.Catalogo.Application.DTOs.Atributo;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FacShopAPI.Catalogo.Infrastructure.Repositories;

public class EfAtributoRepository(ICatalogoContext context) : IAtributoRepository
{
    public async Task<List<AtributoResponse>> ListarPorProdutoAsync(int produtoId)
    {
        var atributos = await context.Atributos
            .Where(a => a.ProdutoId == produtoId)
            .OrderBy(a => a.Chave)
            .ToListAsync();

        return atributos.Select(a => new AtributoResponse
        {
            Id = a.Id,
            ProdutoId = a.ProdutoId,
            Chave = a.Chave,
            Valor = a.Valor,
            DataCriacao = a.DataCriacao
        }).ToList();
    }

    public Task<Atributo?> ObterPorIdAsync(int id) =>
        context.Atributos.FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Atributo> AdicionarAsync(Atributo atributo)
    {
        context.AddAtributo(atributo);
        await context.SaveChangesAsync();
        return atributo;
    }

    public async Task RemoverAsync(int id)
    {
        var atributo = await context.Atributos.FirstOrDefaultAsync(a => a.Id == id);
        if (atributo is not null)
        {
            context.Remove(atributo);
            await context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();
}
