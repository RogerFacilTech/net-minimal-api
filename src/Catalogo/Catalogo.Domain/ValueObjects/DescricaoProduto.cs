using Catalogo.Domain.Common;

namespace Catalogo.Domain.ValueObjects;

public sealed record DescricaoProduto
{
    private DescricaoProduto(string value) => Value = value;
    public string Value { get; }

    public static Result<DescricaoProduto> Criar(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 10)
            return Result<DescricaoProduto>.Fail("Descrição deve ter ao menos 10 caracteres.");
        if (value.Length > 500)
            return Result<DescricaoProduto>.Fail("Descrição não pode exceder 500 caracteres.");
        return Result<DescricaoProduto>.Ok(new DescricaoProduto(value));
    }

    public static DescricaoProduto Reconstituir(string value) => new(value);
    public override string ToString() => Value;
}
