using FacShopAPI.Shared.Kernel;

namespace Catalogo.Domain.ValueObjects;

public sealed record EstoqueProduto
{
    public const int Maximo = 99_999;
    private EstoqueProduto(int value) => Value = value;
    public int Value { get; }

    public static Result<EstoqueProduto> Criar(int value)
    {
        if (value < 0)
            return Result<EstoqueProduto>.Fail("Estoque não pode ser negativo.");
        if (value > Maximo)
            return Result<EstoqueProduto>.Fail($"Estoque não pode exceder {Maximo} unidades.");
        return Result<EstoqueProduto>.Ok(new EstoqueProduto(value));
    }

    public static EstoqueProduto Reconstituir(int value) => new(value);
    public static implicit operator int(EstoqueProduto estoque) => estoque.Value;
}
