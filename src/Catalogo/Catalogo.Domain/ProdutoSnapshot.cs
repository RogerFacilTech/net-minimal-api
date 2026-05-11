namespace Catalogo.Domain;

public sealed record ProdutoSnapshot(
    int Id,
    string Nome,
    decimal Preco,
    int EstoqueDisponivel
);