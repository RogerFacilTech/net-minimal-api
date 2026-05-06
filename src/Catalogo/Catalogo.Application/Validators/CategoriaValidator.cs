using FacShopAPI.Catalogo.Application.DTOs.Categoria;
using FluentValidation;

namespace FacShopAPI.Catalogo.Application.Validators;

public class CriarCategoriaValidator : AbstractValidator<CriarCategoriaRequest>
{
    public CriarCategoriaValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter ao menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Nome não pode exceder 100 caracteres.");

        RuleFor(c => c.CategoriaPaiId)
            .GreaterThan(0).When(c => c.CategoriaPaiId.HasValue)
            .WithMessage("CategoriaPaiId inválido.");
    }
}

public class RenomearCategoriaValidator : AbstractValidator<RenomearCategoriaRequest>
{
    public RenomearCategoriaValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter ao menos 2 caracteres.")
            .MaximumLength(100).WithMessage("Nome não pode exceder 100 caracteres.");
    }
}
