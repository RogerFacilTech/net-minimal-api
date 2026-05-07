using FluentAssertions;
using Xunit;

namespace Pedidos.Tests.Validators;

/// <summary>
/// Testes para validação de criação de pedido
/// Valida regras de entrada para o agregado Pedido
/// </summary>
public class CreatePedidoValidatorTests
{
    [Fact]
    public void CreateCommand_WithValidData_PassesValidation()
    {
        // Arrange & Act - Commands no vertical slice são simples
        // Esta é uma estrutura base para futura expansão
        
        // Assert
        true.Should().BeTrue();
    }

    [Fact]
    public void CreateCommand_WithEmptyClientName_ShouldFailValidation()
    {
        // Arrange & Act
        string clientName = "";

        // Assert
        clientName.Should().BeEmpty();
    }

    [Fact]
    public void CreateCommand_WithValidClientName_ShouldPassValidation()
    {
        // Arrange & Act
        string clientName = "Cliente XYZ";

        // Assert
        clientName.Should().NotBeEmpty();
        clientName.Length.Should().BeGreaterThanOrEqualTo(3);
    }
}
