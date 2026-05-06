namespace FacShopAPI.Catalogo.Application.DTOs.Variante;

public class CriarVarianteRequest
{
    public int ProdutoId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoAdicional { get; set; }
    public int Estoque { get; set; }
}

public class AtualizarPrecoVarianteRequest
{
    public decimal PrecoAdicional { get; set; }
}

public class AtualizarEstoqueVarianteRequest
{
    public int Estoque { get; set; }
}

public class VarianteResponse
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoAdicional { get; set; }
    public int Estoque { get; set; }
    public bool Ativa { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}
