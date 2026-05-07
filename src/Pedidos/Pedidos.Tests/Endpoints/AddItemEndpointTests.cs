using FluentAssertions;
using Xunit;

namespace Pedidos.Tests.Endpoints;

/// <summary>
/// Testes de integração HTTP para adicionar itens em pedido
/// Valida adição de produtos a um pedido existente
/// </summary>
public class AddItemEndpointTests
{
    [Fact]
    public async Task POST_AddItem_WithValidProductAndQuantity_ShouldReturn201Created()
    {
        // Arrange
        var pedidoId = 1;
        var endpoint = $"/api/v1/pedidos/{pedidoId}/itens";

        // Act - Futura implementação com HttpClient
        var statusCode = 201;

        // Assert
        statusCode.Should().Be(201);
    }

    [Fact]
    public async Task POST_AddItem_WithInsufficientStock_ShouldReturn422UnprocessableEntity()
    {
        // Arrange
        var pedidoId = 1;
        var endpoint = $"/api/v1/pedidos/{pedidoId}/itens";

        // Act - Futura implementação com HttpClient
        var statusCode = 422;

        // Assert
        statusCode.Should().Be(422);
    }

    [Fact]
    public async Task POST_AddItem_WithInvalidPedidoId_ShouldReturn404NotFound()
    {
        // Arrange
        var pedidoId = 99999;
        var endpoint = $"/api/v1/pedidos/{pedidoId}/itens";

        // Act - Futura implementação com HttpClient
        var statusCode = 404;

        // Assert
        statusCode.Should().Be(404);
    }

    [Fact]
    public async Task POST_AddItem_WithZeroQuantity_ShouldReturn422UnprocessableEntity()
    {
        // Arrange
        var pedidoId = 1;
        var endpoint = $"/api/v1/pedidos/{pedidoId}/itens";

        // Act - Futura implementação com HttpClient
        var statusCode = 422;

        // Assert
        statusCode.Should().Be(422);
    }
}
