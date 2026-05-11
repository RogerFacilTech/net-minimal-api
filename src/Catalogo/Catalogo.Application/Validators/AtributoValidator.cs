using FacShopAPI.Catalogo.Application.DTOs.Atributo;
using FluentValidation;

namespace FacShopAPI.Catalogo.Application.Validators;

public class CriarAtributoValidator : AbstractValidator<CriarAtributoRequest>
{
    public CriarAtributoValidator()
    {
        RuleFor(a => a.ProdutoId).GreaterThan(0).WithMessage("ProdutoId inválido.");
        RuleFor(a => a.Chave).NotEmpty().WithMessage("Chave é obrigatória.")
            .MaximumLength(100).WithMessage("Chave não pode exceder 100 caracteres.");
        RuleFor(a => a.Valor).NotEmpty().WithMessage("Valor é obrigatório.")
            .MaximumLength(500).WithMessage("Valor não pode exceder 500 caracteres.");
    }
}
