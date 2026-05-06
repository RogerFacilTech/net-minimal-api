using ProdutosAPI.Shared.Common;
using ProdutosAPI.Pedidos.Common;

namespace ProdutosAPI.Pedidos.GetPedido;

public class GetPedidoEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/pedidos/{id:int}", async (
            int id,
            GetPedidoHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetPedidoQuery(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("ObterPedido")
        .WithTags("Pedidos")
        .WithSummary("Obter pedido por ID")
        .Produces<PedidoResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
