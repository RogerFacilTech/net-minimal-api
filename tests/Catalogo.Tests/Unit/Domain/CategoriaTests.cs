using Catalogo.Domain;
using FluentAssertions;
using Xunit;

namespace Catalogo.Tests.Unit.Domain;

public class CategoriaTests
{
    [Fact]
    public void Criar_NomeValido_RetornaSucesso()
    {
        var result = Categoria.Criar("Eletrônicos");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Nome.Should().Be("Eletrônicos");
        result.Value.Slug.Should().Be("eletronicos");
        result.Value.Ativa.Should().BeTrue();
        result.Value.CategoriaPaiId.Should().BeNull();
    }

    [Fact]
    public void Criar_ComCategoriaPaiId_DefineCategoriaPai()
    {
        var result = Categoria.Criar("Notebooks", categoriaPaiId: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CategoriaPaiId.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void Criar_NomeInvalido_RetornaFalha(string nome)
    {
        var result = Categoria.Criar(nome);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Criar_NomeMuitoLongo_RetornaFalha()
    {
        var result = Categoria.Criar(new string('A', 101));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Renomear_NomeValido_AtualizaNomeESlug()
    {
        var categoria = Categoria.Criar("Eletrônicos").Value!;

        var result = categoria.Renomear("Computadores & Periféricos");

        result.IsSuccess.Should().BeTrue();
        categoria.Nome.Should().Be("Computadores & Periféricos");
        categoria.Slug.Should().Be("computadores-perifericos");
    }

    [Fact]
    public void Desativar_CategoriaAtiva_DesativaComSucesso()
    {
        var categoria = Categoria.Criar("Eletrônicos").Value!;

        var result = categoria.Desativar();

        result.IsSuccess.Should().BeTrue();
        categoria.Ativa.Should().BeFalse();
    }

    [Fact]
    public void Desativar_CategoriaJaInativa_RetornaFalha()
    {
        var categoria = Categoria.Criar("Eletrônicos").Value!;
        categoria.Desativar();

        var result = categoria.Desativar();

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("Café & Chá", "cafe-cha")]
    [InlineData("Roupas Femininas", "roupas-femininas")]
    [InlineData("  Livros  ", "livros")]
    public void Criar_SlugGeradoCorretamente(string nome, string slugEsperado)
    {
        var result = Categoria.Criar(nome);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Slug.Should().Be(slugEsperado);
    }
}
