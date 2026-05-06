using FluentValidation;

namespace ProdutosAPI.Pedidos.CreatePedido;

public class CreatePedidoValidator : AbstractValidator<CreatePedidoCommand>
{
    public CreatePedidoValidator()
    {
        RuleFor(x => x.Itens)
            .NotEmpty()
            .WithMessage("Pedido deve conter ao menos um item.");

        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ProdutoId)
                .GreaterThan(0)
                .WithMessage("ProdutoId inválido.");

            item.RuleFor(i => i.Quantidade)
                .InclusiveBetween(1, 999)
                .WithMessage("Quantidade deve estar entre 1 e 999.");
        });
    }
}
