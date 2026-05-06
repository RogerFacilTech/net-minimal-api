using ProdutosAPI.Shared.Common;

namespace Catalogo.Domain;

public class Atributo
{
    private Atributo() { }

    public int Id { get; private set; }
    public int ProdutoId { get; private set; }
    public string Chave { get; private set; } = "";
    public string Valor { get; private set; } = "";
    public DateTime DataCriacao { get; private set; }

    public static Result<Atributo> Criar(int produtoId, string chave, string valor)
    {
        if (produtoId <= 0) return Result<Atributo>.Fail("ProdutoId inválido.");
        if (string.IsNullOrWhiteSpace(chave) || chave.Length > 50)
            return Result<Atributo>.Fail("Chave deve ter entre 1 e 50 caracteres.");
        if (string.IsNullOrWhiteSpace(valor) || valor.Length > 200)
            return Result<Atributo>.Fail("Valor deve ter entre 1 e 200 caracteres.");

        return Result<Atributo>.Ok(new Atributo
        {
            ProdutoId = produtoId,
            Chave = chave.Trim(),
            Valor = valor.Trim(),
            DataCriacao = DateTime.UtcNow
        });
    }

    public Result Atualizar(string chave, string valor)
    {
        if (string.IsNullOrWhiteSpace(chave) || chave.Length > 50)
            return Result.Fail("Chave deve ter entre 1 e 50 caracteres.");
        if (string.IsNullOrWhiteSpace(valor) || valor.Length > 200)
            return Result.Fail("Valor deve ter entre 1 e 200 caracteres.");
        Chave = chave.Trim();
        Valor = valor.Trim();
        return Result.Ok();
    }
}
