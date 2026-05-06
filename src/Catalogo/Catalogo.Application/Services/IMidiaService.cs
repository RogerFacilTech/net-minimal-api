using ProdutosAPI.Shared.Common;
using ProdutosAPI.Catalogo.Application.DTOs.Midia;

namespace ProdutosAPI.Catalogo.Application.Services;

public interface IMidiaService
{
    Task<List<MidiaResponse>> ListarPorProdutoAsync(int produtoId);
    Task<Result<MidiaResponse>> CriarAsync(CriarMidiaRequest request);
    Task<Result<MidiaResponse>> AtualizarOrdemAsync(int id, AtualizarOrdemMidiaRequest request);
    Task<Result> RemoverAsync(int id);
}
