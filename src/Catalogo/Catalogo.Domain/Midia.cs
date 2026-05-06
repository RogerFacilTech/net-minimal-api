using Catalogo.Domain.Common;

namespace Catalogo.Domain;

public enum TipoMidia { Imagem, Video, Documento }

public class Midia
{
    private Midia() { }

    public int Id { get; private set; }
    public int ProdutoId { get; private set; }
    public string Url { get; private set; } = "";
    public TipoMidia Tipo { get; private set; }
    public int Ordem { get; private set; }
    public DateTime DataCriacao { get; private set; }

    public static Result<Midia> Criar(int produtoId, string url, TipoMidia tipo, int ordem = 0)
    {
        if (produtoId <= 0) return Result<Midia>.Fail("ProdutoId inválido.");
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return Result<Midia>.Fail("URL inválida. Deve ser uma URL absoluta.");
        if (url.Length > 500) return Result<Midia>.Fail("URL não pode exceder 500 caracteres.");
        if (ordem < 0) return Result<Midia>.Fail("Ordem não pode ser negativa.");

        return Result<Midia>.Ok(new Midia
        {
            ProdutoId = produtoId,
            Url = url.Trim(),
            Tipo = tipo,
            Ordem = ordem,
            DataCriacao = DateTime.UtcNow
        });
    }

    public Result AtualizarOrdem(int novaOrdem)
    {
        if (novaOrdem < 0) return Result.Fail("Ordem não pode ser negativa.");
        Ordem = novaOrdem;
        return Result.Ok();
    }
}
