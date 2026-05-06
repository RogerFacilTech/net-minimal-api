using System.Text.RegularExpressions;
using ProdutosAPI.Shared.Common;

namespace Catalogo.Domain.ValueObjects;

public sealed record SKU
{
    private static readonly Regex FormatoValido = new(@"^[A-Z0-9\-]+$", RegexOptions.Compiled);

    private SKU(string valor) => Valor = valor;
    public string Valor { get; }

    public static Result<SKU> Criar(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return Result<SKU>.Fail("SKU não pode ser vazio.");
        valor = valor.Trim().ToUpperInvariant();
        if (valor.Length < 6 || valor.Length > 20)
            return Result<SKU>.Fail("SKU deve ter entre 6 e 20 caracteres.");
        if (!FormatoValido.IsMatch(valor))
            return Result<SKU>.Fail("SKU deve conter apenas letras maiúsculas, números e hífens.");
        return Result<SKU>.Ok(new SKU(valor));
    }

    public static SKU Reconstituir(string valor) => new(valor);
    public override string ToString() => Valor;
}
