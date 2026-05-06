using FacShopAPI.Catalogo.Application.DTOs.Variante;
using FluentValidation;

namespace FacShopAPI.Catalogo.Application.Validators;

public class CriarVarianteValidator : AbstractValidator<CriarVarianteRequest>
{
    public CriarVarianteValidator()
    {
        RuleFor(v => v.ProdutoId).GreaterThan(0).WithMessage("ProdutoId inválido.");
        RuleFor(v => v.Sku).NotEmpty().WithMessage("SKU é obrigatório.")
            .MinimumLength(6).WithMessage("SKU deve ter ao menos 6 caracteres.")
            .MaximumLength(20).WithMessage("SKU não pode exceder 20 caracteres.")
            .Matches(@"^[A-Za-z0-9\-]+$").WithMessage("SKU deve conter apenas letras, números e hífens.");
        RuleFor(v => v.Descricao).NotEmpty().WithMessage("Descrição é obrigatória.")
            .MaximumLength(200).WithMessage("Descrição não pode exceder 200 caracteres.");
        RuleFor(v => v.PrecoAdicional).GreaterThan(0).WithMessage("Preço adicional deve ser maior que zero.");
        RuleFor(v => v.Estoque).GreaterThanOrEqualTo(0).WithMessage("Estoque não pode ser negativo.");
    }
}
