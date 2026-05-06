using Catalogo.Domain.ValueObjects;
using FacShopAPI.Shared.Common;

namespace Catalogo.Domain;

public class Variante
{
    private Variante() { }

    public int Id { get; private set; }
    public int ProdutoId { get; private set; }
    public SKU Sku { get; private set; } = null!;
    public string Descricao { get; private set; } = "";
    public PrecoProduto PrecoAdicional { get; private set; } = null!;
    public EstoqueProduto Estoque { get; private set; } = null!;
    public bool Ativa { get; private set; } = true;
    public DateTime DataCriacao { get; private set; }
    public DateTime DataAtualizacao { get; private set; }

    public static Result<Variante> Criar(
        int produtoId, string sku, string descricao,
        decimal precoAdicional, int estoque)
    {
        if (produtoId <= 0)
            return Result<Variante>.Fail("ProdutoId inválido.");
        if (string.IsNullOrWhiteSpace(descricao) || descricao.Length > 200)
            return Result<Variante>.Fail("Descrição deve ter entre 1 e 200 caracteres.");

        var skuResult = SKU.Criar(sku);
        if (!skuResult.IsSuccess)
            return Result<Variante>.Fail(skuResult.Error!);

        var precoResult = PrecoProduto.Criar(precoAdicional);
        if (!precoResult.IsSuccess)
            return Result<Variante>.Fail(precoResult.Error!);

        var estoqueResult = EstoqueProduto.Criar(estoque);
        if (!estoqueResult.IsSuccess)
            return Result<Variante>.Fail(estoqueResult.Error!);

        var agora = DateTime.UtcNow;
        return Result<Variante>.Ok(new Variante
        {
            ProdutoId = produtoId,
            Sku = skuResult.Value!,
            Descricao = descricao.Trim(),
            PrecoAdicional = precoResult.Value!,
            Estoque = estoqueResult.Value!,
            Ativa = true,
            DataCriacao = agora,
            DataAtualizacao = agora
        });
    }

    public Result AtualizarPreco(decimal novoPrecoAdicional)
    {
        var result = PrecoProduto.Criar(novoPrecoAdicional);
        if (!result.IsSuccess) return Result.Fail(result.Error!);
        PrecoAdicional = result.Value!;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result AtualizarEstoque(int quantidade)
    {
        var result = EstoqueProduto.Criar(quantidade);
        if (!result.IsSuccess) return Result.Fail(result.Error!);
        Estoque = result.Value!;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result Desativar()
    {
        if (!Ativa) return Result.Fail("Variante já está inativa.");
        Ativa = false;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }
}
