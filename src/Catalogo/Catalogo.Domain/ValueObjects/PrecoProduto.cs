using FacShopAPI.Shared.Common;

namespace Catalogo.Domain.ValueObjects;

public sealed record PrecoProduto
{
    public const decimal Minimo = 0.01m;
    private PrecoProduto(decimal value) => Value = value;
    public decimal Value { get; }

    public static Result<PrecoProduto> Criar(decimal value)
    {
        if (value < Minimo)
            return Result<PrecoProduto>.Fail("Preço deve ser maior que zero.");
        return Result<PrecoProduto>.Ok(new PrecoProduto(value));
    }

    public static PrecoProduto Reconstituir(decimal value)
    {
        var result = Criar(value);
        if (!result.IsSuccess || result.Value is null)
            throw new InvalidOperationException(result.Error);
        return result.Value;
    }

    public override string ToString() => Value.ToString("0.00");
    public static implicit operator decimal(PrecoProduto preco) => preco.Value;
}
