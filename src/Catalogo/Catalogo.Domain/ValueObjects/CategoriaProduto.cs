using FacShopAPI.Shared.Kernel;

namespace Catalogo.Domain.ValueObjects;

public sealed record CategoriaProduto
{
    private static readonly string[] CategoriasValidas =
        ["Eletrônicos", "Livros", "Roupas", "Alimentos", "Outros"];

    private CategoriaProduto(string value) => Value = value;
    public string Value { get; }

    public static Result<CategoriaProduto> Criar(string value)
    {
        if (!CategoriasValidas.Contains(value))
            return Result<CategoriaProduto>.Fail(
                $"Categoria inválida. Válidas: {string.Join(", ", CategoriasValidas)}");
        return Result<CategoriaProduto>.Ok(new CategoriaProduto(value));
    }

    public static CategoriaProduto Reconstituir(string value) => new(value);
    public override string ToString() => Value;
}
