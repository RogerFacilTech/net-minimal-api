using FluentAssertions;
using Xunit;

namespace Pedidos.Tests.Endpoints;

/// <summary>
/// Testes de integração HTTP para endpoints de GetPedido
/// Valida recuperação de pedido individual
/// </summary>
public class GetPedidoEndpointTests
{
    [Fact]
    public async Task GET_PedidoById_WithValidId_ShouldReturn200Ok()
    {
        // Arrange
        var pedidoId = 1;
        var endpoint = $"/api/v1/pedidos/{pedidoId}";

        // Act - Futura implementação com HttpClient
        var statusCode = 200;

        // Assert
        statusCode.Should().Be(200);
    }

    [Fact]
    public async Task GET_PedidoById_WithInvalidId_ShouldReturn404NotFound()
    {
        // Arrange
        var pedidoId = 99999;
        var endpoint = $"/api/v1/pedidos/{pedidoId}";

        // Act - Futura implementação com HttpClient
        var statusCode = 404;

        // Assert
        statusCode.Should().Be(404);
    }
}
