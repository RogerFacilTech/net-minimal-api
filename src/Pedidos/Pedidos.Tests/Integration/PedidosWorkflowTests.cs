using FacShopAPI.Pedidos.Domain;
using FluentAssertions;
using Pedidos.Tests.Builders;
using Xunit;

namespace Pedidos.Tests.Integration;

/// <summary>
/// Testes de fluxo de trabalho completo (end-to-end)
/// Simula cenários reais de uso do domínio de Pedidos
/// </summary>
public class PedidosWorkflowTests
{
    [Fact]
    public void CompleteWorkflow_CreateConfirmCancel_AllStepsSucceed()
    {
        // Arrange - Preparar dados
        var pedido = Pedido.Criar();
        var produto = ProdutoTestBuilder.Padrao()
            .ComPreco(50m)
            .ComEstoque(5)
            .Build();

        // Act - Fluxo completo
        // 1. Criar pedido (já feito)
        pedido.Status.Should().Be(StatusPedido.Rascunho);

        // 2. Adicionar 2 itens (total 100)
        var adicionarResult = pedido.AdicionarItem(produto, 2);
        adicionarResult.IsSuccess.Should().BeTrue();
        pedido.Total.Should().Be(100m);

        // 3. Confirmar pedido
        var confirmarResult = pedido.Confirmar();
        confirmarResult.IsSuccess.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Confirmado);

        // 4. Cancelar (mesmo sendo confirmado, regra de domínio permite)
        var cancelarResult = pedido.Cancelar("Cliente solicitou");
        cancelarResult.IsSuccess.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Cancelado);
        pedido.MotivoCancelamento.Should().Be("Cliente solicitou");
    }

    [Fact]
    public void RejectedWorkflow_ConfirmWithoutItems_FailsAsExpected()
    {
        // Arrange - Pedido sem itens
        var pedido = Pedido.Criar();

        // Act - Tentar confirmar sem itens
        var result = pedido.Confirmar();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
        pedido.Status.Should().Be(StatusPedido.Rascunho); // Estado não muda
    }

    [Fact]
    public void MultipleItems_WithDifferentProducts_CalculatesCorrectTotal()
    {
        // Arrange - Vários produtos com preços diferentes
        var pedido = Pedido.Criar();
        var produtoA = ProdutoTestBuilder.Padrao()
            .ComPreco(25m)
            .ComEstoque(10)
            .ComNome("Produto A")
            .Build();
        var produtoB = ProdutoTestBuilder.Padrao()
            .ComPreco(75m)
            .ComEstoque(10)
            .ComNome("Produto B")
            .Build();

        // Act - Adicionar múltiplos itens
        pedido.AdicionarItem(produtoA, 2); // 50
        pedido.AdicionarItem(produtoB, 1); // 75

        // Assert
        pedido.Total.Should().Be(125m);
        pedido.Itens.Count().Should().Be(2);

        // Confirmar deve passar
        var confirmarResult = pedido.Confirmar();
        confirmarResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void DuplicateProduct_AddsQuantityToExistingItem()
    {
        // Arrange
        var pedido = Pedido.Criar();
        var produto = ProdutoTestBuilder.Padrao()
            .ComPreco(50m)
            .ComEstoque(100)
            .Build();

        // Act - Adicionar mesmo produto duas vezes
        pedido.AdicionarItem(produto, 2); // 100
        pedido.AdicionarItem(produto, 3); // Deve somar = 250

        // Assert
        pedido.Total.Should().Be(250m);
        pedido.Itens.Count().Should().Be(1); // Apenas 1 item (mesmo produto)
    }
}
