using Catalogo.Domain;
using FacShopAPI.Catalogo.Application.Interfaces;
using FacShopAPI.Catalogo.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FacShopAPI.Catalogo.Infrastructure.Repositories;

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
