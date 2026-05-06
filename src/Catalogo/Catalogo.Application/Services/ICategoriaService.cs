using FacShopAPI.Catalogo.Application.DTOs.Categoria;
using FacShopAPI.Shared.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public interface ICategoriaService
{
    Task<List<CategoriaResponse>> ListarAsync();
    Task<CategoriaResponse?> ObterAsync(int id);
    Task<Result<CategoriaResponse>> CriarAsync(CriarCategoriaRequest request);
    Task<Result<CategoriaResponse>> RenomearAsync(int id, RenomearCategoriaRequest request);
    Task<Result> DesativarAsync(int id);
}
