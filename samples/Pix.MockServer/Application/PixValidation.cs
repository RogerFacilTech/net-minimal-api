using System.Globalization;
using System.Text.RegularExpressions;
using Pix.MockServer.Contracts;

namespace Pix.MockServer.Application;

public static class PixValidation
{
    private static readonly Regex DigitsRegex = new("^\\d+$", RegexOptions.Compiled);

    public static Dictionary<string, string[]> ValidateCobranca(CriarCobrancaRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        AddIf(errors, "calendario.expiracao", request.Calendario.Expiracao is < 60 or > 86400,
            "Expiração deve estar entre 60 e 86400 segundos.");

        AddIf(errors, "devedor.nome", string.IsNullOrWhiteSpace(request.Devedor.Nome),
            "Nome do devedor é obrigatório.");

        AddIf(errors, "devedor.cpfCnpj", !IsCpfCnpj(request.Devedor.CpfCnpj),
            "CPF/CNPJ deve conter 11 ou 14 dígitos.");

        AddIf(errors, "devedor.endereco.cep", !IsCep(request.Devedor.Endereco.Cep),
            "CEP deve conter 8 dígitos.");

        AddIf(errors, "recebedor.ispb", string.IsNullOrWhiteSpace(request.Recebedor.Ispb) || request.Recebedor.Ispb.Length != 8,
            "ISPB deve conter 8 caracteres.");

        AddIf(errors, "valor.original", request.Valor.Original <= 0,
            "Valor original deve ser maior que zero.");

        AddIf(errors, "split", request.Split.Count > 10,
            "Split permite no máximo 10 repasses.");

        AddIf(errors, "infoAdicionais", request.InfoAdicionais.Count > 50,
            "Info adicionais permite no máximo 50 entradas.");

        AddIf(errors, "chavePix", string.IsNullOrWhiteSpace(request.ChavePix),
            "Chave Pix é obrigatória.");

        return errors;
    }

    public static Dictionary<string, string[]> ValidateDevolucao(CriarDevolucaoRequest request, decimal valorMaximo)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        AddIf(errors, "txid", string.IsNullOrWhiteSpace(request.Txid),
            "Txid é obrigatório.");

        AddIf(errors, "endToEndId", string.IsNullOrWhiteSpace(request.EndToEndId),
            "EndToEndId é obrigatório.");

        AddIf(errors, "valor", request.Valor <= 0 || request.Valor > valorMaximo,
            $"Valor deve ser maior que zero e menor/igual a {valorMaximo.ToString("F2", CultureInfo.InvariantCulture)}.");

        AddIf(errors, "natureza", string.IsNullOrWhiteSpace(request.Natureza),
            "Natureza é obrigatória.");

        AddIf(errors, "motivo", string.IsNullOrWhiteSpace(request.Motivo),
            "Motivo é obrigatório.");

        return errors;
    }

    private static bool IsCpfCnpj(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length is 11 or 14 && DigitsRegex.IsMatch(digits);
    }

    private static bool IsCep(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 8 && DigitsRegex.IsMatch(digits);
    }

    private static void AddIf(Dictionary<string, string[]> errors, string key, bool condition, string message)
    {
        if (!condition)
        {
            return;
        }

        errors[key] = [message];
    }
}
