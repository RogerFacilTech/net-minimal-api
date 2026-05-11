using FacShopAPI.Catalogo.Application.DTOs.Atributo;
using FacShopAPI.Catalogo.Application.Services;
using Catalogo.API.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catalogo.API.Endpoints.Atributos;

public static class AtributoEndpoints
{
    public static void MapAtributoEndpoints(this RouteGroupBuilder catalogoGroup)
    {
        var group = catalogoGroup.MapGroup("/atributos").WithTags("Catálogo - Atributos");

        group.MapGet("/", Listar).WithName("ListarAtributos")
            .Produces<List<AtributoResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous()
            .RequireRateLimiting("leitura");

        group.MapPost("/", Criar).WithName("CriarAtributo")
            .Accepts<CriarAtributoRequest>("application/json")
            .Produces<AtributoResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");

        group.MapPut("/{id:int}", Atualizar).WithName("AtualizarAtributo")
            .Accepts<AtualizarAtributoRequest>("application/json")
            .Produces<AtributoResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");

        group.MapDelete("/{id:int}", Remover).WithName("RemoverAtributo")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");
    }

    private static async Task<IResult> Listar(IAtributoService service, int produtoId) =>
        Results.Ok(await service.ListarPorProdutoAsync(produtoId));

    private static async Task<IResult> Criar(CriarAtributoRequest request, IAtributoService service,
        IValidator<CriarAtributoRequest> validator)
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
        var result = await service.CriarAsync(request);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Erro", Detail = result.Error! });
        return Results.Created($"/api/v1/catalogo/atributos/{result.Value!.Id}", result.Value);
    }

    private static async Task<IResult> Atualizar(int id, AtualizarAtributoRequest request, IAtributoService service)
    {
        var result = await service.AtualizarAsync(id, request);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "Não encontrado", Detail = result.Error! });
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> Remover(int id, IAtributoService service)
    {
        var result = await service.RemoverAsync(id);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "Não encontrado", Detail = result.Error! });
        return Results.NoContent();
    }
}
