using Catalogo.Domain.Common;
using Catalogo.Domain.ValueObjects;

namespace Catalogo.Domain;

public class Produto
{
    public static readonly decimal PrecoMinimo = PrecoProduto.Minimo;
    public static readonly int EstoqueMaximo = EstoqueProduto.Maximo;

    private Produto() { }

    public int Id { get; private set; }
    public string Nome { get; private set; } = "";
    public DescricaoProduto Descricao { get; private set; } = null!;
    public PrecoProduto Preco { get; private set; } = null!;
    public CategoriaProduto Categoria { get; private set; } = null!;
    public EstoqueProduto Estoque { get; private set; } = null!;
    public bool Ativo { get; private set; } = true;
    public string ContatoEmail { get; private set; } = "";
    public DateTime DataCriacao { get; private set; }
    public DateTime DataAtualizacao { get; private set; }

    public static Result<Produto> Criar(
        string nome, string descricao, decimal preco,
        string categoria, int estoque, string email)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3)
            return Result<Produto>.Fail("Nome deve ter ao menos 3 caracteres.");
        if (string.IsNullOrWhiteSpace(email))
            return Result<Produto>.Fail("Email de contato é obrigatório.");

        var descricaoResult = DescricaoProduto.Criar(descricao);
        if (!descricaoResult.IsSuccess) return Result<Produto>.Fail(descricaoResult.Error!);

        var precoResult = PrecoProduto.Criar(preco);
        if (!precoResult.IsSuccess) return Result<Produto>.Fail(precoResult.Error!);

        var categoriaResult = CategoriaProduto.Criar(categoria);
        if (!categoriaResult.IsSuccess) return Result<Produto>.Fail(categoriaResult.Error!);

        var estoqueResult = EstoqueProduto.Criar(estoque);
        if (!estoqueResult.IsSuccess) return Result<Produto>.Fail(estoqueResult.Error!);

        var agora = DateTime.UtcNow;
        return Result<Produto>.Ok(new Produto
        {
            Nome = nome,
            Descricao = descricaoResult.Value!,
            Preco = precoResult.Value!,
            Categoria = categoriaResult.Value!,
            Estoque = estoqueResult.Value!,
            ContatoEmail = email,
            Ativo = true,
            DataCriacao = agora,
            DataAtualizacao = agora
        });
    }

    public static Produto Reconstituir(
        int id, string nome, string descricao, decimal preco,
        string categoria, int estoque, bool ativo,
        string contatoEmail, DateTime dataCriacao, DateTime dataAtualizacao)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        return new Produto
        {
            Id = id,
            Nome = nome,
            Descricao = DescricaoProduto.Reconstituir(descricao),
            Preco = PrecoProduto.Reconstituir(preco),
            Categoria = CategoriaProduto.Reconstituir(categoria),
            Estoque = EstoqueProduto.Reconstituir(estoque),
            Ativo = ativo,
            ContatoEmail = contatoEmail,
            DataCriacao = dataCriacao,
            DataAtualizacao = dataAtualizacao
        };
    }

    public Result AtualizarPreco(decimal novoPreco)
    {
        var precoResult = PrecoProduto.Criar(novoPreco);
        if (!precoResult.IsSuccess) return Result.Fail(precoResult.Error!);
        if (precoResult.Value!.Value == Preco.Value) return Result.Fail("Novo preço é igual ao preço atual.");
        Preco = precoResult.Value;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result AtualizarDados(string? nome = null, string? descricao = null,
        string? categoria = null, string? email = null)
    {
        if (nome is not null)
        {
            if (nome.Length < 3) return Result.Fail("Nome deve ter ao menos 3 caracteres.");
            Nome = nome;
        }
        if (descricao is not null)
        {
            var r = DescricaoProduto.Criar(descricao);
            if (!r.IsSuccess) return Result.Fail(r.Error!);
            Descricao = r.Value!;
        }
        if (categoria is not null)
        {
            var r = CategoriaProduto.Criar(categoria);
            if (!r.IsSuccess) return Result.Fail(r.Error!);
            Categoria = r.Value!;
        }
        if (email is not null)
        {
            if (string.IsNullOrWhiteSpace(email)) return Result.Fail("Email de contato é obrigatório.");
            ContatoEmail = email;
        }
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result ReporEstoque(int quantidade)
    {
        if (quantidade <= 0) return Result.Fail("Quantidade de reposição deve ser positiva.");
        var novoEstoque = Estoque.Value + quantidade;
        if (novoEstoque > EstoqueMaximo)
            return Result.Fail($"Estoque não pode exceder {EstoqueMaximo} unidades.");
        Estoque = EstoqueProduto.Reconstituir(novoEstoque);
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result Desativar()
    {
        if (!Ativo) return Result.Fail("Produto já está inativo.");
        Ativo = false;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public bool TemEstoqueDisponivel(int qtd) => Ativo && Estoque.Value >= qtd;

    public void AjustarEstoque(int quantidade)
    {
        var r = EstoqueProduto.Criar(quantidade);
        if (!r.IsSuccess) throw new InvalidOperationException(r.Error);
        Estoque = r.Value!;
        DataAtualizacao = DateTime.UtcNow;
    }

    internal void SetIdForTesting(int id) => Id = id;
}
