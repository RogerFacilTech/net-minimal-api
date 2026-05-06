using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FluentValidation;

namespace FacShopAPI.Catalogo.Application.Validators;

public class CriarProdutoValidator : AbstractValidator<CriarProdutoRequest>
{
    public CriarProdutoValidator()
    {
        RuleFor(p => p.Nome).NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(100).WithMessage("Nome não pode exceder 100 caracteres");

        RuleFor(p => p.Descricao).NotEmpty().WithMessage("Descrição é obrigatória")
            .MinimumLength(10).WithMessage("Descrição deve ter no mínimo 10 caracteres")
            .MaximumLength(500).WithMessage("Descrição não pode exceder 500 caracteres");

        RuleFor(p => p.Preco).GreaterThan(0).WithMessage("Preço deve ser maior que zero")
            .LessThan(999999.99m).WithMessage("Preço não pode ser tão alto");

        RuleFor(p => p.Categoria).NotEmpty().WithMessage("Categoria é obrigatória")
            .Must(c => new[] { "Eletrônicos", "Livros", "Roupas", "Alimentos", "Outros" }.Contains(c))
            .WithMessage("Categoria inválida");

        RuleFor(p => p.Estoque).GreaterThanOrEqualTo(0).WithMessage("Estoque não pode ser negativo")
            .LessThan(1000000).WithMessage("Estoque muito alto");

        RuleFor(p => p.ContatoEmail).NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido");
    }
}

public class AtualizarProdutoValidator : AbstractValidator<AtualizarProdutoRequest>
{
    public AtualizarProdutoValidator()
    {
        RuleFor(p => p.Nome).MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(100).When(p => !string.IsNullOrEmpty(p.Nome));

        RuleFor(p => p.Descricao).MinimumLength(10).WithMessage("Descrição deve ter no mínimo 10 caracteres")
            .MaximumLength(500).When(p => !string.IsNullOrEmpty(p.Descricao));

        RuleFor(p => p.Preco).GreaterThan(0).WithMessage("Preço deve ser maior que zero")
            .When(p => p.Preco.HasValue);

        RuleFor(p => p.Categoria)
            .Must(c => new[] { "Eletrônicos", "Livros", "Roupas", "Alimentos", "Outros" }.Contains(c))
            .WithMessage("Categoria inválida").When(p => !string.IsNullOrEmpty(p.Categoria));

        RuleFor(p => p.Estoque).GreaterThanOrEqualTo(0).WithMessage("Estoque não pode ser negativo")
            .When(p => p.Estoque.HasValue);

        RuleFor(p => p.ContatoEmail).EmailAddress().WithMessage("Email inválido")
            .When(p => !string.IsNullOrEmpty(p.ContatoEmail));
    }
}
