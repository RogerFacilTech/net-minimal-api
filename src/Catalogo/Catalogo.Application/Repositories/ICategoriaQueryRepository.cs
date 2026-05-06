using FacShopAPI.Catalogo.Application.DTOs.Categoria;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface ICategoriaQueryRepository
{
    Task<List<CategoriaResponse>> ListarRaizComSubcategoriasAsync();
    Task<CategoriaResponse?> ObterPorIdAsync(int id);
}
