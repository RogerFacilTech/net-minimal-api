namespace FacShopAPI.Catalogo.Application.DTOs.Categoria;

public class CriarCategoriaRequest
{
    public string Nome { get; set; } = string.Empty;
    public int? CategoriaPaiId { get; set; }
}

public class RenomearCategoriaRequest
{
    public string Nome { get; set; } = string.Empty;
}

public class CategoriaResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? CategoriaPaiId { get; set; }
    public bool Ativa { get; set; }
    public DateTime DataCriacao { get; set; }
    public List<CategoriaResponse> Subcategorias { get; set; } = new();
}
