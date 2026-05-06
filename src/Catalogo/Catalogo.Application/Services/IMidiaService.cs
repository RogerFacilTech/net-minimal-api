using FacShopAPI.Catalogo.Application.DTOs.Midia;
using FacShopAPI.Shared.Kernel;

namespace FacShopAPI.Catalogo.Application.Services;

public interface IMidiaService
{
    Task<List<MidiaResponse>> ListarPorProdutoAsync(int produtoId);
    Task<Result<MidiaResponse>> CriarAsync(CriarMidiaRequest request);
    Task<Result<MidiaResponse>> AtualizarOrdemAsync(int id, AtualizarOrdemMidiaRequest request);
    Task<Result> RemoverAsync(int id);
}
