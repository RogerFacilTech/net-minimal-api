using Catalogo.Domain;
using FacShopAPI.Catalogo.Application.DTOs.Atributo;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IAtributoRepository
{
    Task<List<AtributoResponse>> ListarPorProdutoAsync(int produtoId);
    Task<Atributo?> ObterPorIdAsync(int id);
    Task<Atributo> AdicionarAsync(Atributo atributo);
    Task RemoverAsync(int id);
    Task SaveChangesAsync();
}
