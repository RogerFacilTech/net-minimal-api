using Catalogo.Domain;
using ProdutosAPI.Shared.Common;
using ProdutosAPI.Catalogo.Application.DTOs.Atributo;
using ProdutosAPI.Catalogo.Application.Repositories;

namespace ProdutosAPI.Catalogo.Application.Services;

public class AtributoService : IAtributoService
{
    private readonly IAtributoRepository _repo;

    public AtributoService(IAtributoRepository repo) => _repo = repo;

    public Task<List<AtributoResponse>> ListarPorProdutoAsync(int produtoId) =>
        _repo.ListarPorProdutoAsync(produtoId);

    public async Task<Result<AtributoResponse>> CriarAsync(CriarAtributoRequest request)
    {
        var result = Atributo.Criar(request.ProdutoId, request.Chave, request.Valor);
        if (!result.IsSuccess) return Result<AtributoResponse>.Fail(result.Error!);
        var atributo = await _repo.AdicionarAsync(result.Value!);
        return Result<AtributoResponse>.Ok(MapToResponse(atributo));
    }

    public async Task<Result<AtributoResponse>> AtualizarAsync(int id, AtualizarAtributoRequest request)
    {
        var atributo = await _repo.ObterPorIdAsync(id);
        if (atributo is null) return Result<AtributoResponse>.Fail("Atributo não encontrado.");
        var r = atributo.Atualizar(request.Chave, request.Valor);
        if (!r.IsSuccess) return Result<AtributoResponse>.Fail(r.Error!);
        await _repo.SaveChangesAsync();
        return Result<AtributoResponse>.Ok(MapToResponse(atributo));
    }

    public async Task<Result> RemoverAsync(int id)
    {
        var atributo = await _repo.ObterPorIdAsync(id);
        if (atributo is null) return Result.Fail("Atributo não encontrado.");
        await _repo.RemoverAsync(id);
        return Result.Ok();
    }

    private static AtributoResponse MapToResponse(Atributo a) =>
        new() { Id = a.Id, ProdutoId = a.ProdutoId, Chave = a.Chave, Valor = a.Valor, DataCriacao = a.DataCriacao };
}
