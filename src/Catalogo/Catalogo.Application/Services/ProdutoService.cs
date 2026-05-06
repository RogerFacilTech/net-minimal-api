using AutoMapper;
using Catalogo.Domain;
using FacShopAPI.Catalogo.Application.DTOs.Common;
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Catalogo.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace FacShopAPI.Catalogo.Application.Services;

public class ProdutoService : IProdutoService
{
    private readonly IProdutoQueryRepository _queryRepo;
    private readonly IProdutoCommandRepository _commandRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<ProdutoService> _logger;

    public ProdutoService(
        IProdutoQueryRepository queryRepo,
        IProdutoCommandRepository commandRepo,
        IMapper mapper,
        ILogger<ProdutoService> logger)
    {
        _queryRepo = queryRepo;
        _commandRepo = commandRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ProdutoResponse>> ListarProdutosAsync(
        int page, int pageSize, string? categoria = null, string? search = null)
    {
        _logger.LogInformation("Listando produtos - Page: {Page}, PageSize: {PageSize}", page, pageSize);
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (produtos, total) = await _queryRepo.ListarAsync(page, pageSize, categoria, search);
        return new PaginatedResponse<ProdutoResponse>
        {
            Data = produtos.ToList(),
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        };
    }

    public async Task<ProdutoResponse?> ObterProdutoAsync(int id)
    {
        _logger.LogInformation("Obtendo produto com ID: {ProductId}", id);
        var produto = await _queryRepo.ObterPorIdAsync(id);
        if (produto is null) _logger.LogWarning("Produto {ProductId} não encontrado", id);
        return produto;
    }

    public async Task<ProdutoResponse> CriarProdutoAsync(CriarProdutoRequest request)
    {
        _logger.LogInformation("Criando produto: {Nome}", request.Nome);
        var resultado = Produto.Criar(
            request.Nome, request.Descricao, request.Preco,
            request.Categoria, request.Estoque, request.ContatoEmail);
        if (!resultado.IsSuccess) throw new InvalidOperationException(resultado.Error);
        var produto = await _commandRepo.AdicionarAsync(resultado.Value!);
        _logger.LogInformation("Produto criado. ID: {ProductId}", produto.Id);
        return _mapper.Map<ProdutoResponse>(produto);
    }

    public async Task<ProdutoResponse?> AtualizarProdutoAsync(int id, AtualizarProdutoRequest request)
    {
        _logger.LogInformation("Atualizando produto {ProductId}", id);
        var produto = await _commandRepo.ObterPorIdAsync(id);
        if (produto is null) return null;

        if (request.Preco.HasValue)
        {
            var r = produto.AtualizarPreco(request.Preco.Value);
            if (!r.IsSuccess) throw new InvalidOperationException(r.Error);
        }
        if (request.Estoque.HasValue) produto.AjustarEstoque(request.Estoque.Value);

        var r2 = produto.AtualizarDados(request.Nome, request.Descricao, request.Categoria, request.ContatoEmail);
        if (!r2.IsSuccess) throw new InvalidOperationException(r2.Error);
        await _commandRepo.SaveChangesAsync();
        return _mapper.Map<ProdutoResponse>(produto);
    }

    public async Task<ProdutoResponse?> AtualizarCompletoProdutoAsync(int id, CriarProdutoRequest request)
    {
        _logger.LogInformation("Atualizando completamente produto {ProductId}", id);
        var produto = await _commandRepo.ObterPorIdAsync(id);
        if (produto is null) return null;

        if (request.Preco != produto.Preco.Value)
        {
            var r = produto.AtualizarPreco(request.Preco);
            if (!r.IsSuccess) throw new InvalidOperationException(r.Error);
        }
        produto.AjustarEstoque(request.Estoque);
        var r2 = produto.AtualizarDados(request.Nome, request.Descricao, request.Categoria, request.ContatoEmail);
        if (!r2.IsSuccess) throw new InvalidOperationException(r2.Error);
        await _commandRepo.SaveChangesAsync();
        return _mapper.Map<ProdutoResponse>(produto);
    }

    public async Task<bool> DeletarProdutoAsync(int id)
    {
        _logger.LogInformation("Deletando produto {ProductId}", id);
        var deletado = await _commandRepo.DeletarAsync(id);
        if (!deletado) _logger.LogWarning("Produto {ProductId} não encontrado para deleção", id);
        return deletado;
    }
}
