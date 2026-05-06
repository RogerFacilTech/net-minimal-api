using FacShopAPI.Pedidos.Domain;
using FluentAssertions;
using Pedidos.Tests.Builders;
using Xunit;

namespace Pedidos.Tests.Domain;

/// <summary>
/// Testes unitários para o agregado Pedido
/// Valida regras de negócio do domínio rico
/// </summary>
public class PedidoTests
{
    [Fact]
    public void Criar_RetornaPedidoEmRascunho()
    {
        // Act
        var pedido = Pedido.Criar();

        // Assert
        pedido.Status.Should().Be(StatusPedido.Rascunho);
        pedido.Itens.Should().BeEmpty();
        pedido.Total.Should().Be(0m);
    }

    [Fact]
    public void AdicionarItem_ProdutoValido_AdicionaComSucesso()
    {
        // Arrange
        var pedido = Pedido.Criar();
        var produto = ProdutoTestBuilder.Padrao()
            .ComPreco(100m)
            .ComEstoque(10)
            .Build();

        // Act
        var resultado = pedido.AdicionarItem(produto, 2);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        pedido.Itens.Should().HaveCount(1);
        pedido.Total.Should().Be(200m);
    }

    [Fact]
    public void AdicionarItem_PedidoJaConfirmado_RetornaFalha()
    {
        // Arrange
        var pedido = Pedido.Criar();
        var produto = ProdutoTestBuilder.Padrao()
            .ComPreco(100m)
            .ComEstoque(10)
            .Build();
        pedido.AdicionarItem(produto, 1);
        pedido.Confirmar();

        // Act — tentar adicionar item a pedido confirmado
        var resultado = pedido.AdicionarItem(produto, 1);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.Error.Should().Contain("rascunho");
    }

    [Fact]
    public void AdicionarItem_QuantidadeInvalida_RetornaFalha()
    {
        // Arrange
        var pedido = Pedido.Criar();
        var produto = ProdutoTestBuilder.Padrao()
            .ComEstoque(100)
            .ComPreco(50m)
            .Build();

        // Act
        var resultado = pedido.AdicionarItem(produto, 0);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AdicionarItem_ProdutoRepetido_AtualizaQuantidade()
    {
        // Arrange
        var pedido = Pedido.Criar();
        var produto = ProdutoTestBuilder.Padrao()
            .ComEstoque(100)
            .ComPreco(50m)
            .Build();

        pedido.AdicionarItem(produto, 2);

        // Act
        var resultado = pedido.AdicionarItem(produto, 3); // Adicionar mais do mesmo

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        pedido.Itens.Should().HaveCount(1);
        pedido.Total.Should().Be(250m); // (2+3) * 50
    }

    [Fact]
    public void Confirmar_PedidoEmRascunho_MudaStatusParaConfirmado()
    {
        // Arrange
        var pedido = Pedido.Criar();
        var produto = ProdutoTestBuilder.Padrao()
            .ComEstoque(100)
            .ComPreco(50m)
            .Build();
        pedido.AdicionarItem(produto, 1);

        // Act
        var resultado = pedido.Confirmar();

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Confirmado);
        pedido.ConfirmadoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Confirmar_PedidoSemItens_RetornaFalha()
    {
        // Arrange
        var pedido = Pedido.Criar();

        // Act
        var resultado = pedido.Confirmar();

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.Error.Should().Contain("item");
    }

    [Fact]
    public void Confirmar_PedidoComValorMenorQueMinimo_RetornaFalha()
    {
        // Arrange
        var pedido = Pedido.Criar();
        var produto = ProdutoTestBuilder.Padrao()
            .ComEstoque(100)
            .ComPreco(5m) // Preço baixo
            .Build();
        pedido.AdicionarItem(produto, 1); // Total: 5m (menor que 10m mínimo)

        // Act
        var resultado = pedido.Confirmar();

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.Error.Should().Contain("10");
    }

    [Fact]
    public void Cancelar_PedidoEmRascunho_MudaStatusParaCancelado()
    {
        // Arrange
        var pedido = Pedido.Criar();
        var motivo = "Cliente solicitou cancelamento";

        // Act
        var resultado = pedido.Cancelar(motivo);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        pedido.Status.Should().Be(StatusPedido.Cancelado);
        pedido.MotivoCancelamento.Should().Be(motivo);
        pedido.CanceladoEm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Cancelar_PedidoJaCancelado_RetornaFalha()
    {
        // Arrange
        var pedido = Pedido.Criar();
        pedido.Cancelar("Motivo 1");

        // Act
        var resultado = pedido.Cancelar("Motivo 2");

        // Assert
        resultado.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Cancelar_SemMotivo_RetornaFalha()
    {
        // Arrange
        var pedido = Pedido.Criar();

        // Act
        var resultado = pedido.Cancelar("");

        // Assert
        resultado.IsSuccess.Should().BeFalse();
    }
}
