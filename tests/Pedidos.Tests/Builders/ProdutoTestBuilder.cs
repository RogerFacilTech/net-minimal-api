using ProdutosAPI.Pedidos.Domain;

namespace Pedidos.Tests.Builders;

/// <summary>
/// Builder para facilitar criação de dados de teste para ProdutoSnapshot
/// </summary>
public class ProdutoTestBuilder
{
    private static int _nextId = 1;

    private int _id = _nextId++;
    private string _nome = "Produto Teste";
    private decimal _preco = 100m;

    public static ProdutoTestBuilder Padrao() => new();

    public ProdutoTestBuilder ComId(int id)
    {
        _id = id;
        return this;
    }

    public ProdutoTestBuilder ComPreco(decimal preco)
    {
        _preco = preco;
        return this;
    }

    /// <summary>
    /// Mantido para compatibilidade — estoque é validado externamente no Catálogo,
    /// não pertence ao domínio de Pedidos.
    /// </summary>
    public ProdutoTestBuilder ComEstoque(int estoque) => this;

    public ProdutoTestBuilder ComNome(string nome)
    {
        _nome = nome;
        return this;
    }

    public ProdutoSnapshot Build() => new(_id, _nome, _preco);
}
