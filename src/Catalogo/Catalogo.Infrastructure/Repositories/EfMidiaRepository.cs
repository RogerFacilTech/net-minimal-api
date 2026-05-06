using Catalogo.Domain;
using Microsoft.EntityFrameworkCore;
using ProdutosAPI.Catalogo.Application.DTOs.Midia;
using ProdutosAPI.Catalogo.Application.Interfaces;
using ProdutosAPI.Catalogo.Application.Repositories;

namespace ProdutosAPI.Catalogo.Infrastructure.Repositories;

public class EfMidiaRepository(ICatalogoContext context) : IMidiaRepository
{
    public async Task<List<MidiaResponse>> ListarPorProdutoAsync(int produtoId)
    {
        var midias = await context.Midias
            .Where(m => m.ProdutoId == produtoId)
            .OrderBy(m => m.Ordem)
            .ToListAsync();

        return midias.Select(m => new MidiaResponse
        {
            Id = m.Id,
            ProdutoId = m.ProdutoId,
            Url = m.Url,
            Tipo = m.Tipo,
            Ordem = m.Ordem,
            DataCriacao = m.DataCriacao
        }).ToList();
    }

    public Task<Midia?> ObterPorIdAsync(int id) =>
        context.Midias.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<Midia> AdicionarAsync(Midia midia)
    {
        context.AddMidia(midia);
        await context.SaveChangesAsync();
        return midia;
    }

    public async Task RemoverAsync(int id)
    {
        var midia = await context.Midias.FirstOrDefaultAsync(m => m.Id == id);
        if (midia is not null)
        {
            context.Remove(midia);
            await context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();
}
