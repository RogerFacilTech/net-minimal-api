using Catalogo.Domain;
using FacShopAPI.Catalogo.Application.DTOs.Midia;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Shared.Common;

namespace FacShopAPI.Catalogo.Application.Services;

public class MidiaService : IMidiaService
{
    private readonly IMidiaRepository _repo;

    public MidiaService(IMidiaRepository repo) => _repo = repo;

    public Task<List<MidiaResponse>> ListarPorProdutoAsync(int produtoId) =>
        _repo.ListarPorProdutoAsync(produtoId);

    public async Task<Result<MidiaResponse>> CriarAsync(CriarMidiaRequest request)
    {
        var result = Midia.Criar(request.ProdutoId, request.Url, request.Tipo, request.Ordem);
        if (!result.IsSuccess) return Result<MidiaResponse>.Fail(result.Error!);
        var midia = await _repo.AdicionarAsync(result.Value!);
        return Result<MidiaResponse>.Ok(MapToResponse(midia));
    }

    public async Task<Result<MidiaResponse>> AtualizarOrdemAsync(int id, AtualizarOrdemMidiaRequest request)
    {
        var midia = await _repo.ObterPorIdAsync(id);
        if (midia is null) return Result<MidiaResponse>.Fail("Mídia não encontrada.");
        var r = midia.AtualizarOrdem(request.Ordem);
        if (!r.IsSuccess) return Result<MidiaResponse>.Fail(r.Error!);
        await _repo.SaveChangesAsync();
        return Result<MidiaResponse>.Ok(MapToResponse(midia));
    }

    public async Task<Result> RemoverAsync(int id)
    {
        var midia = await _repo.ObterPorIdAsync(id);
        if (midia is null) return Result.Fail("Mídia não encontrada.");
        await _repo.RemoverAsync(id);
        return Result.Ok();
    }

    private static MidiaResponse MapToResponse(Midia m) =>
        new() { Id = m.Id, ProdutoId = m.ProdutoId, Url = m.Url, Tipo = m.Tipo, Ordem = m.Ordem, DataCriacao = m.DataCriacao };
}
