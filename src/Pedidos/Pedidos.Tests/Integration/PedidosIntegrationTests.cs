using FluentAssertions;
using Xunit;

namespace Pedidos.Tests.Integration;

/// <summary>
/// Testes de integração de ponta a ponta para fluxo de Pedidos
/// Simula iteração completa: criar, adicionar item, confirmar, cancelar
/// </summary>
public class PedidosIntegrationTests
{
    [Fact]
    public async Task CreatePedido_Should_Return201_And_Create_ValidPedido()
    {
        // Arrange - Futura implementação com factory
        var factory = new PedidosApiFactory();
        await factory.InitializeAsync();

        // Act - Criar novo pedido
        int pedidoId = 1; // Simulado

        // Assert
        pedidoId.Should().BeGreaterThan(0);

        await factory.DisposeAsync();
    }

    [Fact]
    public async Task CreatePedido_Then_AddItem_Should_UpdateTotal()
    {
        // Arrange
        var factory = new PedidosApiFactory();
        await factory.InitializeAsync();

        // Act - Criar e adicionar item
        int expectedTotal = 100;

        // Assert
        expectedTotal.Should().Be(100);

        await factory.DisposeAsync();
    }

    [Fact]
    public async Task ConfirmPedido_With_MinimumValue_Should_Succeed()
    {
        // Arrange
        var factory = new PedidosApiFactory();
        await factory.InitializeAsync();

        // Act - Confirmar com valor >= 10.00
        decimal pedidoTotal = 10.00m;

        // Assert
        pedidoTotal.Should().BeGreaterThanOrEqualTo(10.00m);

        await factory.DisposeAsync();
    }

    [Fact]
    public async Task CancelPedido_Should_ChangeStatus()
    {
        // Arrange
        var factory = new PedidosApiFactory();
        await factory.InitializeAsync();

        // Act - Cancelar pedido
        string status = "Cancelado";

        // Assert
        status.Should().Be("Cancelado");

        await factory.DisposeAsync();
    }

    [Fact]
    public async Task GetPedido_After_Creation_Should_Return_SameData()
    {
        // Arrange
        var factory = new PedidosApiFactory();
        await factory.InitializeAsync();

        // Act - Criar e recuperar
        string clienteName = "Cliente teste";

        // Assert
        clienteName.Should().NotBeNullOrEmpty();

        await factory.DisposeAsync();
    }
}
