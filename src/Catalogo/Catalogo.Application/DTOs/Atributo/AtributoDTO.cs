namespace FacShopAPI.Catalogo.Application.DTOs.Atributo;

public class CriarAtributoRequest
{
    public int ProdutoId { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}

public class AtualizarAtributoRequest
{
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}

public class AtributoResponse
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}
