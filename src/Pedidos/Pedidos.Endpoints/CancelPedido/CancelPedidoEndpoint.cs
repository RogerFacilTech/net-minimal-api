using FacShopAPI.Pedidos.Common;
using FacShopAPI.Shared.Kernel;
using FacShopAPI.Shared.Web;

namespace FacShopAPI.Pedidos.CancelPedido;

public class CancelPedidoEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/pedidos/{id:int}/cancelar", async (
            int id,
            CancelPedidoRequest request,
            CancelPedidoHandler handler,
            CancellationToken ct) =>
        {
            var cmd = new CancelPedidoCommand(id, request.Motivo);
            var result = await handler.HandleAsync(cmd, ct);

            if (!result.IsSuccess)
            {
                return result.Error!.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)
                    ? Results.NotFound(new { error = result.Error })
                    : Results.BadRequest(new { error = result.Error });
            }
            return Results.Ok(result.Value);
        })
        .WithName("CancelarPedido")
        .WithTags("Pedidos")
        .WithSummary("Cancelar pedido")
        .Produces<PedidoResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .AllowAnonymous();
    }
}
