using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ProdutosAPI.Catalogo.Endpoints.DTOs;
using ProdutosAPI.Catalogo.Application.DTOs.Midia;
using ProdutosAPI.Catalogo.Application.Services;

namespace ProdutosAPI.Catalogo.Endpoints.Endpoints.Midias;

public static class MidiaEndpoints
{
    public static void MapMidiaEndpoints(this RouteGroupBuilder catalogoGroup)
    {
        var group = catalogoGroup.MapGroup("/midias").WithTags("Catálogo - Mídias");

        group.MapGet("/", Listar).WithName("ListarMidias")
            .Produces<List<MidiaResponse>>(StatusCodes.Status200OK)
            .AllowAnonymous()
            .RequireRateLimiting("leitura");

        group.MapPost("/", Criar).WithName("CriarMidia")
            .Accepts<CriarMidiaRequest>("application/json")
            .Produces<MidiaResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");

        group.MapPatch("/{id:int}/ordem", AtualizarOrdem).WithName("AtualizarOrdemMidia")
            .Accepts<AtualizarOrdemMidiaRequest>("application/json")
            .Produces<MidiaResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");

        group.MapDelete("/{id:int}", Remover).WithName("RemoverMidia")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .RequireRateLimiting("escrita");
    }

    private static async Task<IResult> Listar(IMidiaService service, int produtoId) =>
        Results.Ok(await service.ListarPorProdutoAsync(produtoId));

    private static async Task<IResult> Criar(CriarMidiaRequest request, IMidiaService service)
    {
        var result = await service.CriarAsync(request);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new ErrorResponse { Status = 422, Title = "Erro", Detail = result.Error! });
        return Results.Created($"/api/v1/catalogo/midias/{result.Value!.Id}", result.Value);
    }

    private static async Task<IResult> AtualizarOrdem(int id, AtualizarOrdemMidiaRequest request, IMidiaService service)
    {
        var result = await service.AtualizarOrdemAsync(id, request);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "Não encontrado", Detail = result.Error! });
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> Remover(int id, IMidiaService service)
    {
        var result = await service.RemoverAsync(id);
        if (!result.IsSuccess)
            return Results.NotFound(new ErrorResponse { Status = 404, Title = "Não encontrado", Detail = result.Error! });
        return Results.NoContent();
    }
}
