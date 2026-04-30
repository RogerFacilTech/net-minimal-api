using ProdutosAPI.Catalogo.Domain;

namespace Pedidos.Tests.Builders;

/// <summary>
/// Builder para facilitar criação de dados de teste para Produto
/// </summary>
public class ProdutoTestBuilder
{
    private static int _nextId = 1;

    private int _id = _nextId++;
    private string _nome = "Produto Teste";
    private string _descricao = "Descrição de teste";
    private decimal _preco = 100m;
    private string _categoria = "Eletrônicos";
    private int _estoque = 10;
    private string _email = "contato@teste.com";

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

    public ProdutoTestBuilder ComEstoque(int estoque)
    {
        _estoque = estoque;
        return this;
    }

    public ProdutoTestBuilder ComNome(string nome)
    {
        _nome = nome;
        return this;
    }

    public Produto Build()
    {
        var result = Produto.Criar(_nome, _descricao, _preco, _categoria, _estoque, _email);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Produto builder falhou: {result.Error}");

        var produto = result.Value!;
        typeof(Produto).GetProperty("Id")?.SetValue(produto, _id);

        return produto;
    }
}
