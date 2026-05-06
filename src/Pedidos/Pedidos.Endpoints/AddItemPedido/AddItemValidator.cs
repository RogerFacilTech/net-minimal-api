using FluentValidation;

namespace ProdutosAPI.Pedidos.AddItemPedido;

public record AddItemRequest(int ProdutoId, int Quantidade);

public class AddItemValidator : AbstractValidator<AddItemRequest>
{
    public AddItemValidator()
    {
        RuleFor(x => x.ProdutoId).GreaterThan(0).WithMessage("ProdutoId inválido.");
        RuleFor(x => x.Quantidade)
            .InclusiveBetween(1, 999)
            .WithMessage("Quantidade deve estar entre 1 e 999.");
    }
}
