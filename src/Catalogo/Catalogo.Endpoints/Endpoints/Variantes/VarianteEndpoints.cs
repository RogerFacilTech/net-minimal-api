using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ProdutosAPI.Catalogo.Endpoints.DTOs;
using ProdutosAPI.Catalogo.Application.DTOs.Variante;
using ProdutosAPI.Catalogo.Application.Services;

namespace ProdutosAPI.Catalogo.Endpoints.Endpoints.Variantes;

public static class VarianteEndpoints
{
    public static void MapVarianteEndpoints(this RouteGroupBuilder catalogoGroup)
    {
        var group = catalogoGroup.MapGroup("/variantes")
            .WithTags("Catálogo - Variantes");

        group.MapGet("/", ListarVariantes).WithName("ListarVariantes")
            .Produces<List<VarianteResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous()
            .RequireRateLimiting("leitura");

        group.MapGet("/{id:int}", ObterVariante).WithName("ObterVariante")
            .Produces<VarianteResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .AllowAnonymous()
            .RequireRateLimiting("leitura");

        group.MapPost("/", CriarVariante).WithName("CriarVariante")
            .Accepts<CriarVarianteRequest>("application/json")
            .Produces<VarianteResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");

        group.MapPut("/{id:int}", AtualizarPreco).WithName("AtualizarPrecoVariante")
            .Accepts<AtualizarPrecoVarianteRequest>("application/json")
            .Produces<VarianteResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");

        group.MapPatch("/{id:int}/estoque", AtualizarEstoque).WithName("AtualizarEstoqueVariante")
            .Accepts<AtualizarEstoqueVarianteRequest>("application/json")
            .Produces<VarianteResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");

        group.MapDelete("/{id:int}", DesativarVariante).WithName("DesativarVariante")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");
    }

    private static async Task<IResult> ListarVariantes(IVarianteService service, int produtoId)
    {
        var variantes = await service.ListarPorProdutoAsync(produtoId);
        return Results.Ok(variantes);
    }

    private static async Task<IResult> ObterVariante(int id, IVarianteService service)
    {
        var variante = await service.ObterAsync(id);
        if (variante is null)
            return Results.NotFound(new ErrorResponse
            { Status = 404, Title = "Variante não encontrada", Detail = $"Variante {id} não encontrada." });
        return Results.Ok(variante);
    }

    private static async Task<IResult> CriarVariante(
        CriarVarianteRequest request, IVarianteService service, IValidator<CriarVarianteRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            { Status = 422, Title = "Validação falhou",
              Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)) });

        var result = await service.CriarAsync(request);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new ErrorResponse
            { Status = 422, Title = "Regra de negócio violada", Detail = result.Error! });

        return Results.Created($"/api/v1/catalogo/variantes/{result.Value!.Id}", result.Value);
    }

    private static async Task<IResult> AtualizarPreco(int id, AtualizarPrecoVarianteRequest request, IVarianteService service)
    {
        var result = await service.AtualizarPrecoAsync(id, request);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "Variante não encontrada", Detail = result.Error! });
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> AtualizarEstoque(int id, AtualizarEstoqueVarianteRequest request, IVarianteService service)
    {
        var result = await service.AtualizarEstoqueAsync(id, request);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "Variante não encontrada", Detail = result.Error! });
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> DesativarVariante(int id, IVarianteService service)
    {
        var result = await service.DesativarAsync(id);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "Variante não encontrada", Detail = result.Error! });
        return Results.NoContent();
    }
}
