using Catalogo.Domain;
using ProdutosAPI.Shared.Common;
using Microsoft.Extensions.Logging;
using ProdutosAPI.Catalogo.Application.DTOs.Variante;
using ProdutosAPI.Catalogo.Application.Repositories;

namespace ProdutosAPI.Catalogo.Application.Services;

public class VarianteService : IVarianteService
{
    private readonly IVarianteCommandRepository _commandRepo;
    private readonly IVarianteQueryRepository _queryRepo;
    private readonly ILogger<VarianteService> _logger;

    public VarianteService(
        IVarianteCommandRepository commandRepo,
        IVarianteQueryRepository queryRepo,
        ILogger<VarianteService> logger)
    {
        _commandRepo = commandRepo;
        _queryRepo = queryRepo;
        _logger = logger;
    }

    public Task<List<VarianteResponse>> ListarPorProdutoAsync(int produtoId) =>
        _queryRepo.ListarPorProdutoAsync(produtoId);

    public Task<VarianteResponse?> ObterAsync(int id) => _queryRepo.ObterPorIdAsync(id);

    public async Task<Result<VarianteResponse>> CriarAsync(CriarVarianteRequest request)
    {
        var skuExiste = await _commandRepo.SkuExisteParaProdutoAsync(request.ProdutoId, request.Sku.Trim().ToUpperInvariant());
        if (skuExiste)
            return Result<VarianteResponse>.Fail($"SKU '{request.Sku}' já existe para este produto.");

        var result = Variante.Criar(request.ProdutoId, request.Sku, request.Descricao,
            request.PrecoAdicional, request.Estoque);
        if (!result.IsSuccess) return Result<VarianteResponse>.Fail(result.Error!);

        var variante = await _commandRepo.AdicionarAsync(result.Value!);
        _logger.LogInformation("Variante criada. ID: {Id}", variante.Id);
        return Result<VarianteResponse>.Ok(MapToResponse(variante));
    }

    public async Task<Result<VarianteResponse>> AtualizarPrecoAsync(int id, AtualizarPrecoVarianteRequest request)
    {
        var variante = await _commandRepo.ObterPorIdAsync(id);
        if (variante is null) return Result<VarianteResponse>.Fail("Variante não encontrada.");
        var r = variante.AtualizarPreco(request.PrecoAdicional);
        if (!r.IsSuccess) return Result<VarianteResponse>.Fail(r.Error!);
        await _commandRepo.SaveChangesAsync();
        return Result<VarianteResponse>.Ok(MapToResponse(variante));
    }

    public async Task<Result<VarianteResponse>> AtualizarEstoqueAsync(int id, AtualizarEstoqueVarianteRequest request)
    {
        var variante = await _commandRepo.ObterPorIdAsync(id);
        if (variante is null) return Result<VarianteResponse>.Fail("Variante não encontrada.");
        var r = variante.AtualizarEstoque(request.Estoque);
        if (!r.IsSuccess) return Result<VarianteResponse>.Fail(r.Error!);
        await _commandRepo.SaveChangesAsync();
        return Result<VarianteResponse>.Ok(MapToResponse(variante));
    }

    public async Task<Result> DesativarAsync(int id)
    {
        var variante = await _commandRepo.ObterPorIdAsync(id);
        if (variante is null) return Result.Fail("Variante não encontrada.");
        var r = variante.Desativar();
        if (!r.IsSuccess) return r;
        await _commandRepo.SaveChangesAsync();
        return Result.Ok();
    }

    private static VarianteResponse MapToResponse(Variante v) => new()
    {
        Id = v.Id,
        ProdutoId = v.ProdutoId,
        Sku = v.Sku.Valor,
        Descricao = v.Descricao,
        PrecoAdicional = v.PrecoAdicional.Value,
        Estoque = v.Estoque.Value,
        Ativa = v.Ativa,
        DataCriacao = v.DataCriacao,
        DataAtualizacao = v.DataAtualizacao
    };
}
