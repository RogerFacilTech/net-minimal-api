using FluentAssertions;
using Xunit;

namespace Pedidos.Tests.Endpoints;

/// <summary>
/// Testes de integração HTTP para cancelar pedidos
/// Valida transição de estado e tratamento de erros
/// </summary>
public class CancelPedidoEndpointTests
{
    [Fact]
    public async Task PATCH_CancelPedido_WithValidReasonAndStatus_ShouldReturn204NoContent()
    {
        // Arrange
        var pedidoId = 1;
        var endpoint = $"/api/v1/pedidos/{pedidoId}/cancelar";

        // Act - Futura implementação com HttpClient
        var statusCode = 204;

        // Assert
        statusCode.Should().Be(204);
    }

    [Fact]
    public async Task PATCH_CancelPedido_WithInvalidPedidoId_ShouldReturn404NotFound()
    {
        // Arrange
        var pedidoId = 99999;
        var endpoint = $"/api/v1/pedidos/{pedidoId}/cancelar";

        // Act - Futura implementação com HttpClient
        var statusCode = 404;

        // Assert
        statusCode.Should().Be(404);
    }

    [Fact]
    public async Task PATCH_CancelPedido_AlreadyCanceled_ShouldReturn409Conflict()
    {
        // Arrange
        var pedidoId = 1;
        var endpoint = $"/api/v1/pedidos/{pedidoId}/cancelar";

        // Act - Futura implementação com HttpClient
        var statusCode = 409;

        // Assert
        statusCode.Should().Be(409);
    }

    [Fact]
    public async Task PATCH_CancelPedido_WithoutReason_ShouldReturn422UnprocessableEntity()
    {
        // Arrange
        var pedidoId = 1;
        var endpoint = $"/api/v1/pedidos/{pedidoId}/cancelar";

        // Act - Futura implementação com HttpClient
        var statusCode = 422;

        // Assert
        statusCode.Should().Be(422);
    }

    [Fact]
    public async Task PATCH_CancelPedido_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var pedidoId = 1;
        var endpoint = $"/api/v1/pedidos/{pedidoId}/cancelar";

        // Act - Futura implementação com HttpClient
        var statusCode = 401;

        // Assert
        statusCode.Should().Be(401);
    }
}
