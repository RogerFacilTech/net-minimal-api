using Catalogo.Domain;

namespace FacShopAPI.Catalogo.Application.Repositories;

public interface IProdutoCommandRepository
{
    Task<Produto?> ObterPorIdAsync(int id);
    Task<Produto> AdicionarAsync(Produto produto);
    Task<bool> DeletarAsync(int id);
    Task SaveChangesAsync();
}
