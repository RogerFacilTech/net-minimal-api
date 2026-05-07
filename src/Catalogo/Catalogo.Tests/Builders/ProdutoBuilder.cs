using Catalogo.Domain;

namespace Catalogo.Tests.Builders;

public class ProdutoBuilder
{
    private string _nome = "Produto Teste";
    private string _descricao = "Descrição de teste";
    private decimal _preco = 100m;
    private string _categoria = "Eletrônicos";
    private int _estoque = 10;
    private string _email = "contato@teste.com";

    public static ProdutoBuilder Padrao() => new();

    public ProdutoBuilder ComPreco(decimal preco) { _preco = preco; return this; }
    public ProdutoBuilder ComEstoque(int estoque) { _estoque = estoque; return this; }
    public ProdutoBuilder ComNome(string nome) { _nome = nome; return this; }

    public Produto Build()
    {
        var result = Produto.Criar(_nome, _descricao, _preco, _categoria, _estoque, _email);
        if (!result.IsSuccess) throw new InvalidOperationException(result.Error);
        return result.Value!;
    }
}
