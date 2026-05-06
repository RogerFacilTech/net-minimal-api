using Catalogo.Domain;
using Catalogo.Domain.Common;
using FluentAssertions;
using Catalogo.Tests.Builders;
using Xunit;

namespace Catalogo.Tests.Unit.Domain;

public class ProdutoTests
{
    [Fact]
    public void Criar_ComDadosValidos_RetornaProduto()
    {
        var result = Produto.Criar("Notebook", "Descricao completa do produto", 1000m, "Eletrônicos", 5, "a@b.com");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Nome.Should().Be("Notebook");
        result.Value.Preco.Value.Should().Be(1000m);
    }

    [Fact]
    public void Criar_NomeCurto_RetornaFalha()
    {
        var result = Produto.Criar("AB", "Desc", 100m, "Livros", 1, "a@b.com");
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("3");
    }

    [Fact]
    public void Criar_PrecoZero_RetornaFalha()
    {
        var result = Produto.Criar("Notebook", "Desc", 0m, "Livros", 1, "a@b.com");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Criar_EstoqueNegativo_RetornaFalha()
    {
        var result = Produto.Criar("Notebook", "Desc", 100m, "Livros", -1, "a@b.com");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AtualizarPreco_ValorValido_Atualiza()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        var result = produto.AtualizarPreco(200m);

        result.IsSuccess.Should().BeTrue();
        produto.Preco.Value.Should().Be(200m);
    }

    [Fact]
    public void AtualizarPreco_MesmoPreco_RetornaFalha()
    {
        var produto = ProdutoBuilder.Padrao().ComPreco(100m).Build();
        var result = produto.AtualizarPreco(100m);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AtualizarPreco_PrecoZero_RetornaFalha()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        var result = produto.AtualizarPreco(0m);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ReporEstoque_QuantidadePositiva_Adiciona()
    {
        var produto = ProdutoBuilder.Padrao().ComEstoque(10).Build();
        var result = produto.ReporEstoque(5);

        result.IsSuccess.Should().BeTrue();
        produto.Estoque.Value.Should().Be(15);
    }

    [Fact]
    public void ReporEstoque_QuantidadeNegativa_RetornaFalha()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        var result = produto.ReporEstoque(-1);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ReporEstoque_ExcedeMaximo_RetornaFalha()
    {
        var produto = ProdutoBuilder.Padrao().ComEstoque(99_990).Build();
        var result = produto.ReporEstoque(20);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("99999");
    }

    [Fact]
    public void Desativar_ProdutoAtivo_Desativa()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        var result = produto.Desativar();

        result.IsSuccess.Should().BeTrue();
        produto.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Desativar_ProdutoJaInativo_RetornaFalha()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        produto.Desativar();
        var result = produto.Desativar();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void TemEstoqueDisponivel_AtivoComEstoque_RetornaTrue()
    {
        var produto = ProdutoBuilder.Padrao().ComEstoque(10).Build();
        produto.TemEstoqueDisponivel(5).Should().BeTrue();
    }

    [Fact]
    public void TemEstoqueDisponivel_EstoqueInsuficiente_RetornaFalse()
    {
        var produto = ProdutoBuilder.Padrao().ComEstoque(2).Build();
        produto.TemEstoqueDisponivel(5).Should().BeFalse();
    }

    [Fact]
    public void TemEstoqueDisponivel_ProdutoInativo_RetornaFalse()
    {
        var produto = ProdutoBuilder.Padrao().ComEstoque(100).Build();
        produto.Desativar();
        produto.TemEstoqueDisponivel(1).Should().BeFalse();
    }

    [Fact]
    public void AjustarEstoque_ValorValido_AtribuiEstoque()
    {
        var produto = ProdutoBuilder.Padrao().ComEstoque(10).Build();
        produto.AjustarEstoque(50);
        produto.Estoque.Value.Should().Be(50);
    }

    [Fact]
    public void AjustarEstoque_ValorNegativo_ThrowsException()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        var act = () => produto.AjustarEstoque(-1);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AjustarEstoque_ExcedeMaximo_ThrowsException()
    {
        var produto = ProdutoBuilder.Padrao().Build();
        var act = () => produto.AjustarEstoque(100_000);
        act.Should().Throw<InvalidOperationException>();
    }
}
