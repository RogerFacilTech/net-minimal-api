using Catalogo.Domain.Common;
using ProdutosAPI.Catalogo.Application.DTOs.Atributo;

namespace ProdutosAPI.Catalogo.Application.Services;

public interface IAtributoService
{
    Task<List<AtributoResponse>> ListarPorProdutoAsync(int produtoId);
    Task<Result<AtributoResponse>> CriarAsync(CriarAtributoRequest request);
    Task<Result<AtributoResponse>> AtualizarAsync(int id, AtualizarAtributoRequest request);
    Task<Result> RemoverAsync(int id);
}
