using FacShopAPI.Catalogo.Application.DTOs.Variante;
using FacShopAPI.Shared.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public interface IVarianteService
{
    Task<List<VarianteResponse>> ListarPorProdutoAsync(int produtoId);
    Task<VarianteResponse?> ObterAsync(int id);
    Task<Result<VarianteResponse>> CriarAsync(CriarVarianteRequest request);
    Task<Result<VarianteResponse>> AtualizarPrecoAsync(int id, AtualizarPrecoVarianteRequest request);
    Task<Result<VarianteResponse>> AtualizarEstoqueAsync(int id, AtualizarEstoqueVarianteRequest request);
    Task<Result> DesativarAsync(int id);
}
