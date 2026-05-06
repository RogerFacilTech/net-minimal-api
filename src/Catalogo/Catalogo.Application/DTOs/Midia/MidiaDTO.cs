using Catalogo.Domain;

namespace ProdutosAPI.Catalogo.Application.DTOs.Midia;

public class CriarMidiaRequest
{
    public int ProdutoId { get; set; }
    public string Url { get; set; } = string.Empty;
    public TipoMidia Tipo { get; set; }
    public int Ordem { get; set; }
}

public class AtualizarOrdemMidiaRequest
{
    public int Ordem { get; set; }
}

public class MidiaResponse
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string Url { get; set; } = string.Empty;
    public TipoMidia Tipo { get; set; }
    public int Ordem { get; set; }
    public DateTime DataCriacao { get; set; }
}
