using FacShopAPI.Catalogo.Application.DTOs.Categoria;
using FacShopAPI.Catalogo.Application.Services;
using FacShopAPI.Catalogo.Endpoints.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FacShopAPI.Catalogo.Endpoints.Endpoints.Categorias;

public static class CategoriaEndpoints
{
    public static void MapCategoriaEndpoints(this RouteGroupBuilder catalogoGroup)
    {
        var group = catalogoGroup.MapGroup("/categorias")
            .WithTags("Catálogo - Categorias");

        group.MapGet("/", ListarCategorias).WithName("ListarCategorias")
            .Produces<List<CategoriaResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous()
            .RequireRateLimiting("leitura");

        group.MapGet("/{id:int}", ObterCategoria).WithName("ObterCategoria")
            .Produces<CategoriaResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .AllowAnonymous()
            .RequireRateLimiting("leitura");

        group.MapPost("/", CriarCategoria).WithName("CriarCategoria")
            .Accepts<CriarCategoriaRequest>("application/json")
            .Produces<CategoriaResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");

        group.MapPut("/{id:int}", RenomearCategoria).WithName("RenomearCategoria")
            .Accepts<RenomearCategoriaRequest>("application/json")
            .Produces<CategoriaResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");

        group.MapDelete("/{id:int}", DesativarCategoria).WithName("DesativarCategoria")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");
    }

    private static async Task<IResult> ListarCategorias(ICategoriaService service)
    {
        var categorias = await service.ListarAsync();
        return Results.Ok(categorias);
    }

    private static async Task<IResult> ObterCategoria(int id, ICategoriaService service)
    {
        var categoria = await service.ObterAsync(id);
        if (categoria is null)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404,
                Title = "Categoria não encontrada",
                Detail = $"Categoria com ID {id} não encontrada.",
                Instance = $"/api/v1/catalogo/categorias/{id}"
            });
        return Results.Ok(categoria);
    }

    private static async Task<IResult> CriarCategoria(
        CriarCategoriaRequest request, ICategoriaService service,
        IValidator<CriarCategoriaRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Validação falhou",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))
            });

        var result = await service.CriarAsync(request);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Regra de negócio violada",
                Detail = result.Error!
            });

        return Results.Created($"/api/v1/catalogo/categorias/{result.Value!.Id}", result.Value);
    }

    private static async Task<IResult> RenomearCategoria(
        int id, RenomearCategoriaRequest request, ICategoriaService service,
        IValidator<RenomearCategoriaRequest> validator)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.UnprocessableEntity(new ErrorResponse
            {
                Status = 422,
                Title = "Validação falhou",
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))
            });

        var result = await service.RenomearAsync(id, request);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse
            {
                Status = 404,
                Title = "Categoria não encontrada",
                Detail = result.Error!
            });

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> DesativarCategoria(int id, ICategoriaService service)
    {
        var result = await service.DesativarAsync(id);
        if (!result.IsSuccess)
            return result.Error!.Contains("não encontrada")
                ? Results.NotFound(new ErrorResponse { Status = 404, Title = "Categoria não encontrada", Detail = result.Error })
                : Results.UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Operação não permitida", Detail = result.Error });

        return Results.NoContent();
    }
}
