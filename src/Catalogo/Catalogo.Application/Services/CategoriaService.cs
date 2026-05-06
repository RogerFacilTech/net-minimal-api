using Catalogo.Domain;
using FacShopAPI.Catalogo.Application.DTOs.Categoria;
using FacShopAPI.Catalogo.Application.Repositories;
using FacShopAPI.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace FacShopAPI.Catalogo.Application.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ICategoriaCommandRepository _commandRepo;
    private readonly ICategoriaQueryRepository _queryRepo;
    private readonly ILogger<CategoriaService> _logger;

    public CategoriaService(
        ICategoriaCommandRepository commandRepo,
        ICategoriaQueryRepository queryRepo,
        ILogger<CategoriaService> logger)
    {
        _commandRepo = commandRepo;
        _queryRepo = queryRepo;
        _logger = logger;
    }

    public Task<List<CategoriaResponse>> ListarAsync() =>
        _queryRepo.ListarRaizComSubcategoriasAsync();

    public Task<CategoriaResponse?> ObterAsync(int id) =>
        _queryRepo.ObterPorIdAsync(id);

    public async Task<Result<CategoriaResponse>> CriarAsync(CriarCategoriaRequest request)
    {
        if (request.CategoriaPaiId.HasValue)
        {
            var pai = await _commandRepo.ObterPorIdAsync(request.CategoriaPaiId.Value);
            if (pai is null)
                return Result<CategoriaResponse>.Fail("Categoria pai não encontrada.");

            if (pai.CategoriaPaiId.HasValue)
                return Result<CategoriaResponse>.Fail("Hierarquia máxima de 2 níveis atingida. Não é possível criar subcategoria de uma subcategoria.");

            var paiTemFilhos = await _commandRepo.CategoriaPaiTemSubcategoriasAsync(request.CategoriaPaiId.Value);
            // permite criar subcategoria mesmo se já tem outras (sem limite de largura)
        }

        var result = Categoria.Criar(request.Nome, request.CategoriaPaiId);
        if (!result.IsSuccess) return Result<CategoriaResponse>.Fail(result.Error!);

        var categoria = await _commandRepo.AdicionarAsync(result.Value!);
        _logger.LogInformation("Categoria criada. ID: {Id}", categoria.Id);

        var response = await _queryRepo.ObterPorIdAsync(categoria.Id);
        return Result<CategoriaResponse>.Ok(response!);
    }

    public async Task<Result<CategoriaResponse>> RenomearAsync(int id, RenomearCategoriaRequest request)
    {
        var categoria = await _commandRepo.ObterPorIdAsync(id);
        if (categoria is null)
            return Result<CategoriaResponse>.Fail("Categoria não encontrada.");

        var result = categoria.Renomear(request.Nome);
        if (!result.IsSuccess) return Result<CategoriaResponse>.Fail(result.Error!);

        await _commandRepo.SaveChangesAsync();

        var response = await _queryRepo.ObterPorIdAsync(id);
        return Result<CategoriaResponse>.Ok(response!);
    }

    public async Task<Result> DesativarAsync(int id)
    {
        var categoria = await _commandRepo.ObterPorIdAsync(id);
        if (categoria is null) return Result.Fail("Categoria não encontrada.");

        var temProdutos = await _commandRepo.TemProdutosAtivosAsync(id);
        if (temProdutos) return Result.Fail("Não é possível desativar categoria com produtos ativos.");

        var result = categoria.Desativar();
        if (!result.IsSuccess) return result;

        await _commandRepo.SaveChangesAsync();
        return Result.Ok();
    }
}
