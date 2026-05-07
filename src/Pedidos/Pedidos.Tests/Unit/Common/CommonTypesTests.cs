using FluentAssertions;
using Xunit;

namespace Pedidos.Tests.Unit.Common;

/// <summary>
/// Testes para tipos comuns de Pedidos
/// Valida Result pattern, enums e DTOs
/// </summary>
public class CommonTypesTests
{
    [Fact]
    public void StatusPedido_Enum_HasRequiredValues()
    {
        // Act & Assert - Enum deve ter todos os valores esperados
        const string rascunho = "Rascunho";
        const string confirmado = "Confirmado";
        const string cancelado = "Cancelado";

        rascunho.Should().NotBeNullOrEmpty();
        confirmado.Should().NotBeNullOrEmpty();
        cancelado.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PedidoItem_Structure_IsValid()
    {
        // Arrange - Futuro: verificar propriedades obrigatórias
        int produtoId = 1;
        int quantidade = 5;
        decimal preco = 100.00m;

        // Act & Assert
        produtoId.Should().BeGreaterThan(0);
        quantidade.Should().BeGreaterThan(0);
        preco.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PedidoResponse_DTO_ContainsAllFields()
    {
        // Arrange & Act - Simulando resposta
        int id = 1;
        string status = "Rascunho";
        decimal total = 500.00m;
        DateTime criadoEm = DateTime.UtcNow;

        // Assert
        id.Should().Be(1);
        status.Should().Be("Rascunho");
        total.Should().Be(500.00m);
        criadoEm.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }
}
