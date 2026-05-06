using FacShopAPI.Catalogo.Application.DTOs.Variante;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IVarianteQueryRepository
{
    Task<List<VarianteResponse>> ListarPorProdutoAsync(int produtoId);
    Task<VarianteResponse?> ObterPorIdAsync(int id);
}
