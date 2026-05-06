using Catalogo.Domain;
using Microsoft.EntityFrameworkCore;
using ProdutosAPI.Catalogo.Application.Interfaces;
using ProdutosAPI.Catalogo.Application.Repositories;

namespace ProdutosAPI.Catalogo.Infrastructure.Repositories;

public class EfProdutoCommandRepository(ICatalogoContext context) : IProdutoCommandRepository
{
    public Task<Produto?> ObterPorIdAsync(int id) =>
        context.Produtos.FirstOrDefaultAsync(p => p.Id == id && p.Ativo);

    public async Task<Produto> AdicionarAsync(Produto produto)
    {
        context.AddProduto(produto);
        await context.SaveChangesAsync();
        return produto;
    }

    public async Task<bool> DeletarAsync(int id)
    {
        var produto = await context.Produtos.FirstOrDefaultAsync(p => p.Id == id && p.Ativo);
        if (produto is null) return false;
        produto.Desativar();
        await context.SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();
}
