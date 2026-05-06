using Catalogo.Domain;
using ProdutosAPI.Catalogo.Application.DTOs.Midia;

namespace ProdutosAPI.Catalogo.Application.Repositories;

public interface IMidiaRepository
{
    Task<List<MidiaResponse>> ListarPorProdutoAsync(int produtoId);
    Task<Midia?> ObterPorIdAsync(int id);
    Task<Midia> AdicionarAsync(Midia midia);
    Task RemoverAsync(int id);
    Task SaveChangesAsync();
}
