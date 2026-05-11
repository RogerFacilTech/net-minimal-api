using FacShopAPI.Catalogo.Application.DTOs.Common;
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FacShopAPI.Catalogo.Application.Services;
using Catalogo.API.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catalogo.API.Endpoints.Produtos;

public static class ProdutoEndpoints
{
    public static void MapProdutoEndpoints(this RouteGroupBuilder catalogoGroup)
    {
        var group = catalogoGroup.MapGroup("/produtos")
            .WithTags("Catálogo - Produtos");

        group.MapGet("/", ListarProdutos).WithName("ListarProdutos")
            .Produces<PaginatedResponse<ProdutoResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous()
            .RequireRateLimiting("leitura");

        group.MapGet("/{id:int}", ObterProduto).WithName("ObterProduto")
            .Produces<ProdutoResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .AllowAnonymous()
            .RequireRateLimiting("leitura");

        group.MapPost("/", CriarProduto).WithName("CriarProduto")
            .Accepts<CriarProdutoRequest>("application/json")
            .Produces<ProdutoResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization()
            .RequireRateLimiting("criacao-produto");

        group.MapPut("/{id:int}", AtualizarCompletoProduto).WithName("AtualizarCompletoProduto")
            .Accepts<CriarProdutoRequest>("application/json")
            .Produces<ProdutoResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");

        group.MapPatch("/{id:int}", AtualizarParcialProduto).WithName("AtualizarParcialProduto")
            .Accepts<AtualizarProdutoRequest>("application/json")
            .Produces<ProdutoResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");

        group.MapDelete("/{id:int}", DeletarProduto).WithName("DeletarProduto")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");
    }

    private static async Task<IResult> ListarProdutos(
        IProdutoService service, int page = 1, int pageSize = 20,
        string? categoria = null, string? search = null)
    {
        var resultado = await service.ListarProdutosAsync(page, pageSize, categoria, search);
        return Results.Ok(resultado);
    }

    private static async Task<IResult> ObterProduto(int id, IProdutoService service)
    {
        var produto = await service.ObterProdutoAsync(id);
        if (produto is null)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404,
                Title = "Produto não encontrado",
                Detail = $"Produto com ID {id} não encontrado.",
                Type = "https://api.example.com/errors/not-found",
                Instance = $"/api/v1/catalogo/produtos/{id}"
            });
        return Results.Ok(produto);
    }

    private static async Task<IResult> CriarProduto(
        CriarProdutoRequest request, IProdutoService service,
        IValidator<CriarProdutoRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Validação falhou",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                Type = "https://api.example.com/errors/validation"
            });
        var produto = await service.CriarProdutoAsync(request);
        return Results.Created($"/api/v1/catalogo/produtos/{produto.Id}", produto);
    }

    private static async Task<IResult> AtualizarCompletoProduto(
        int id, CriarProdutoRequest request, IProdutoService service,
        IValidator<CriarProdutoRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Validação falhou",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                Type = "https://api.example.com/errors/validation"
            });
        var produto = await service.AtualizarCompletoProdutoAsync(id, request);
        if (produto is null)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404,
                Title = "Produto não encontrado",
                Detail = $"Produto com ID {id} não encontrado.",
                Type = "https://api.example.com/errors/not-found",
                Instance = $"/api/v1/catalogo/produtos/{id}"
            });
        return Results.Ok(produto);
    }

    private static async Task<IResult> AtualizarParcialProduto(
        int id, AtualizarProdutoRequest request, IProdutoService service,
        IValidator<AtualizarProdutoRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Validação falhou",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                Type = "https://api.example.com/errors/validation"
            });
        var produto = await service.AtualizarProdutoAsync(id, request);
        if (produto is null)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404,
                Title = "Produto não encontrado",
                Detail = $"Produto com ID {id} não encontrado.",
                Type = "https://api.example.com/errors/not-found",
                Instance = $"/api/v1/catalogo/produtos/{id}"
            });
        return Results.Ok(produto);
    }

    private static async Task<IResult> DeletarProduto(int id, IProdutoService service)
    {
        var deletado = await service.DeletarProdutoAsync(id);
        if (!deletado)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404,
                Title = "Produto não encontrado",
                Detail = $"Produto com ID {id} não encontrado.",
                Type = "https://api.example.com/errors/not-found",
                Instance = $"/api/v1/catalogo/produtos/{id}"
            });
        return Results.NoContent();
    }
}
