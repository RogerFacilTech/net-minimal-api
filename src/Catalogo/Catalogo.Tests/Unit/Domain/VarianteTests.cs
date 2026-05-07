using Catalogo.Domain;
using FluentAssertions;
using Xunit;

namespace Catalogo.Tests.Unit.Domain;

public class VarianteTests
{
    [Fact]
    public void Criar_DadosValidos_RetornaSucesso()
    {
        var result = Variante.Criar(1, "PROD-001", "P / Azul", 0.01m, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sku.Valor.Should().Be("PROD-001");
        result.Value.ProdutoId.Should().Be(1);
        result.Value.Ativa.Should().BeTrue();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("PRODUTO-SKU-MUITO-LONGO-DEMAIS")]
    [InlineData("sku_minusculo")]
    public void Criar_SkuInvalido_RetornaFalha(string sku)
    {
        var result = Variante.Criar(1, sku, "Descrição", 10m, 5);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Criar_PrecoZero_RetornaFalha()
    {
        var result = Variante.Criar(1, "SKU-001", "Descrição", 0m, 5);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Criar_EstoqueNegativo_RetornaFalha()
    {
        var result = Variante.Criar(1, "SKU-001", "Descrição", 10m, -1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void AtualizarPreco_PrecoValido_AtualizaComSucesso()
    {
        var variante = Variante.Criar(1, "SKU-001", "Descrição", 10m, 5).Value!;

        var result = variante.AtualizarPreco(20m);

        result.IsSuccess.Should().BeTrue();
        variante.PrecoAdicional.Value.Should().Be(20m);
    }

    [Fact]
    public void AtualizarEstoque_EstoqueValido_AtualizaComSucesso()
    {
        var variante = Variante.Criar(1, "SKU-001", "Descrição", 10m, 5).Value!;

        var result = variante.AtualizarEstoque(100);

        result.IsSuccess.Should().BeTrue();
        variante.Estoque.Value.Should().Be(100);
    }

    [Fact]
    public void Desativar_VarianteAtiva_DesativaComSucesso()
    {
        var variante = Variante.Criar(1, "SKU-001", "Descrição", 10m, 5).Value!;

        var result = variante.Desativar();

        result.IsSuccess.Should().BeTrue();
        variante.Ativa.Should().BeFalse();
    }
}
