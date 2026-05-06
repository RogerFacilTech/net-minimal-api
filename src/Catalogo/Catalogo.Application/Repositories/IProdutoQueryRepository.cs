using FacShopAPI.Catalogo.Application.DTOs.Produto;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IProdutoQueryRepository
{
    Task<ProdutoResponse?> ObterPorIdAsync(int id);
    Task<(IReadOnlyList<ProdutoResponse> Items, int Total)> ListarAsync(
        int page, int pageSize, string? categoria = null, string? search = null);
}
