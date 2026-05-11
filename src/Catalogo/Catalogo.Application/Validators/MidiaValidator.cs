using FacShopAPI.Catalogo.Application.DTOs.Midia;
using FluentValidation;

namespace FacShopAPI.Catalogo.Application.Validators;

public class CriarMidiaValidator : AbstractValidator<CriarMidiaRequest>
{
    public CriarMidiaValidator()
    {
        RuleFor(m => m.ProdutoId).GreaterThan(0).WithMessage("ProdutoId inválido.");
        RuleFor(m => m.Url).NotEmpty().WithMessage("URL é obrigatória.")
            .MaximumLength(2048).WithMessage("URL não pode exceder 2048 caracteres.");
        RuleFor(m => m.Ordem).GreaterThanOrEqualTo(0).WithMessage("Ordem não pode ser negativa.");
    }
}
