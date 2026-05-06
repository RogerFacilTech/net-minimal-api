using FluentValidation;
using ProdutosAPI.Shared.Common;
using ProdutosAPI.Pedidos.Common;

namespace ProdutosAPI.Pedidos.CreatePedido;

public class CreatePedidoEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/pedidos", async (
            CreatePedidoCommand cmd,
            CreatePedidoHandler handler,
            IValidator<CreatePedidoCommand> validator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(cmd, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await handler.HandleAsync(cmd, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/pedidos/{result.Value!.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CriarPedido")
        .WithTags("Pedidos")
        .WithSummary("Criar pedido")
        .WithDescription("Cria um novo pedido em status Rascunho com os itens fornecidos")
        .Accepts<CreatePedidoCommand>("application/json")
        .Produces<PedidoResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .RequireAuthorization();
    }
}
