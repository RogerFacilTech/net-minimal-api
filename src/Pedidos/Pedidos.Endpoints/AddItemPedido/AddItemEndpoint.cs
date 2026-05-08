using FacShopAPI.Pedidos.Common;
using FacShopAPI.Shared.Kernel;
using FacShopAPI.Shared.Web;
using FluentValidation;

namespace FacShopAPI.Pedidos.AddItemPedido;

public class AddItemEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/pedidos/{id:int}/itens", async (
            int id,
            AddItemRequest request,
            AddItemHandler handler,
            IValidator<AddItemRequest> validator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var cmd = new AddItemCommand(id, request.ProdutoId, request.Quantidade);
            var result = await handler.HandleAsync(cmd, ct);

            if (!result.IsSuccess)
            {
                return result.Error!.Contains("não encontrado", StringComparison.OrdinalIgnoreCase)
                    ? Results.NotFound(new { error = result.Error })
                    : Results.BadRequest(new { error = result.Error });
            }
            return Results.Ok(result.Value);
        })
        .WithName("AdicionarItemPedido")
        .WithTags("Pedidos")
        .WithSummary("Adicionar item a pedido em rascunho")
        .Produces<PedidoResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
    }
}
