using Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IVarianteCommandRepository
{
    Task<Variante?> ObterPorIdAsync(int id);
    Task<Variante> AdicionarAsync(Variante variante);
    Task SaveChangesAsync();
    Task<bool> SkuExisteParaProdutoAsync(int produtoId, string sku);
}
