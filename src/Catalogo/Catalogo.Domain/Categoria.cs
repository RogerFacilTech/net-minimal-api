using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FacShopAPI.Shared.Kernel;

namespace Catalogo.Domain;

public class Categoria
{
    private Categoria() { }

    public int Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public int? CategoriaPaiId { get; private set; }
    public bool Ativa { get; private set; } = true;
    public DateTime DataCriacao { get; private set; }
    public DateTime DataAtualizacao { get; private set; }

    public static Result<Categoria> Criar(string nome, int? categoriaPaiId = null)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length < 2)
            return Result<Categoria>.Fail("Nome da categoria deve ter ao menos 2 caracteres.");
        if (nome.Trim().Length > 100)
            return Result<Categoria>.Fail("Nome da categoria não pode exceder 100 caracteres.");

        var agora = DateTime.UtcNow;
        return Result<Categoria>.Ok(new Categoria
        {
            Nome = nome.Trim(),
            Slug = GerarSlug(nome),
            CategoriaPaiId = categoriaPaiId,
            Ativa = true,
            DataCriacao = agora,
            DataAtualizacao = agora
        });
    }

    public Result Renomear(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome) || novoNome.Trim().Length < 2)
            return Result.Fail("Nome da categoria deve ter ao menos 2 caracteres.");
        if (novoNome.Trim().Length > 100)
            return Result.Fail("Nome da categoria não pode exceder 100 caracteres.");

        Nome = novoNome.Trim();
        Slug = GerarSlug(novoNome);
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result Desativar()
    {
        if (!Ativa) return Result.Fail("Categoria já está inativa.");
        Ativa = false;
        DataAtualizacao = DateTime.UtcNow;
        return Result.Ok();
    }

    private static string GerarSlug(string nome)
    {
        var normalized = nome.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var slug = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return slug;
    }
}
