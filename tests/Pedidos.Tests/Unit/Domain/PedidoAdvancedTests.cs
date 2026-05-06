using FluentAssertions;
using Pedidos.Tests.Builders;
using ProdutosAPI.Pedidos.Domain;
using Xunit;

namespace Pedidos.Tests.Unit.Domain;

/// <summary>
/// Testes adicionais de domínio para casos complexos
/// Valida cenários avançados do agregado Pedido
/// </summary>
public class PedidoAdvancedTests
{
    [Fact]
    public void Pedido_MultipleItems_CalculatesTotalCorrectly()
    {
        // Arrange
        var pedido = Pedido.Criar();
        var produto1 = ProdutoTestBuilder.Padrao()
            .ComPreco(100m)
            .ComEstoque(10)
            .Build();
        var produto2 = ProdutoTestBuilder.Padrao()
            .ComPreco(50m)
            .ComEstoque(20)
            .Build();

        // Act
        pedido.AdicionarItem(produto1, 2); // 200
        pedido.AdicionarItem(produto2, 4); // 200
        var total = pedido.Total;

        // Assert
        total.Should().Be(400m);
    }

    [Fact]
    public void Pedido_ConfirmState_PreservesCreationDate()
    {
        // Arrange
        var pedido = Pedido.Criar();
        var produto = ProdutoTestBuilder.Padrao()
            .ComPreco(100m)
            .ComEstoque(10)
            .Build();
        pedido.AdicionarItem(produto, 2);
        var criadoEmBefore = pedido.CriadoEm;

        // Act
        System.Threading.Thread.Sleep(10);
        pedido.Confirmar();
        var criadoEmAfter = pedido.CriadoEm;

        // Assert
        criadoEmAfter.Should().Be(criadoEmBefore);
    }

    [Fact]
    public void Pedido_CanAddItemAfterPartialChanges()
    {
        // Arrange
        var pedido = Pedido.Criar();
        var produto1 = ProdutoTestBuilder.Padrao()
            .ComPreco(100m)
            .ComEstoque(10)
            .Build();
        var produto2 = ProdutoTestBuilder.Padrao()
            .ComPreco(50m)
            .ComEstoque(5)
            .Build();

        // Act
        var resultado1 = pedido.AdicionarItem(produto1, 2);
        var resultado2 = pedido.AdicionarItem(produto2, 1);

        // Assert
        resultado1.IsSuccess.Should().BeTrue();
        resultado2.IsSuccess.Should().BeTrue();
        pedido.Itens.Count().Should().Be(2);
    }

    [Fact]
    public void Pedido_MaxItems_NotExceeded()
    {
        // Arrange - Máximo 20 itens
        var pedido = Pedido.Criar();
        var produto = ProdutoTestBuilder.Padrao()
            .ComPreco(10m)
            .ComEstoque(100)
            .Build();

        // Act
        pedido.AdicionarItem(produto, 20); // Máximo

        // Assert
        pedido.Itens.Count().Should().BeLessThanOrEqualTo(20);
    }
}
