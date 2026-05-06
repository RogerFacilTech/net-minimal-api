using Catalogo.Domain;
using FacShopAPI.Catalogo.Application.DTOs.Midia;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IMidiaRepository
{
    Task<List<MidiaResponse>> ListarPorProdutoAsync(int produtoId);
    Task<Midia?> ObterPorIdAsync(int id);
    Task<Midia> AdicionarAsync(Midia midia);
    Task RemoverAsync(int id);
    Task SaveChangesAsync();
}
