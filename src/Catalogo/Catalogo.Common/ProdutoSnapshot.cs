namespace ProdutosAPI.Catalogo.Common;

public sealed record ProdutoSnapshot(
    int Id,
    string Nome,
    decimal Preco,
    int EstoqueDisponivel
);