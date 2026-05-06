using Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface ICategoriaCommandRepository
{
    Task<Categoria?> ObterPorIdAsync(int id);
    Task<Categoria> AdicionarAsync(Categoria categoria);
    Task SaveChangesAsync();
    Task<bool> TemProdutosAtivosAsync(int categoriaId);
    Task<bool> CategoriaPaiTemSubcategoriasAsync(int categoriaPaiId);
}
