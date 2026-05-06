using Catalogo.Domain;
using Microsoft.EntityFrameworkCore;
using ProdutosAPI.Catalogo.Application.Interfaces;
using ProdutosAPI.Catalogo.Application.Repositories;

namespace ProdutosAPI.Catalogo.Infrastructure.Repositories;

public class EfCategoriaCommandRepository(ICatalogoContext context) : ICategoriaCommandRepository
{
    public Task<Categoria?> ObterPorIdAsync(int id) =>
        context.Categorias.FirstOrDefaultAsync(c => c.Id == id && c.Ativa);

    public async Task<Categoria> AdicionarAsync(Categoria categoria)
    {
        context.AddCategoria(categoria);
        await context.SaveChangesAsync();
        return categoria;
    }

    public async Task SaveChangesAsync() => await context.SaveChangesAsync();

    public Task<bool> TemProdutosAtivosAsync(int categoriaId) =>
        Task.FromResult(false); // TODO: quando Produto.CategoriaId for FK, implementar

    public Task<bool> CategoriaPaiTemSubcategoriasAsync(int categoriaPaiId) =>
        context.Categorias.AnyAsync(c => c.CategoriaPaiId == categoriaPaiId && c.Ativa);
}
