using FacShopAPI.Shared.Common;

namespace FacShopAPI.Pedidos.Domain;

public class Pedido
{
    public static readonly int MaxItensPorPedido = 20;
    public static readonly decimal ValorMinimoConfirmacao = 10.00m;

    public int Id { get; private set; }
    public StatusPedido Status { get; private set; } = StatusPedido.Rascunho;
    public decimal Total { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? ConfirmadoEm { get; private set; }
    public DateTime? CanceladoEm { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    private readonly List<PedidoItem> _itens = [];
    public IReadOnlyCollection<PedidoItem> Itens => _itens.AsReadOnly();

    private Pedido() { }

    public static Pedido Criar() => new() { CriadoEm = DateTime.UtcNow };

    public Result AdicionarItem(ProdutoSnapshot produto, int quantidade)
    {
        if (Status != StatusPedido.Rascunho)
            return Result.Fail("Itens só podem ser adicionados a pedidos em rascunho.");

        if (quantidade < 1 || quantidade > 999)
            return Result.Fail("Quantidade deve estar entre 1 e 999.");


        // Validação de estoque deve ser feita via API do Catálogo antes de chamar este método
        var existente = _itens.FirstOrDefault(i => i.ProdutoId == produto.Id);
        if (existente is not null)
        {
            existente.IncrementarQuantidade(quantidade);
        }
        else
        {
            if (_itens.Count >= MaxItensPorPedido)
                return Result.Fail($"Pedido não pode ter mais de {MaxItensPorPedido} itens distintos.");

            _itens.Add(PedidoItem.Criar(produto, quantidade));
        }

        RecalcularTotal();
        return Result.Ok();
    }

    public Result Confirmar()
    {
        if (Status != StatusPedido.Rascunho)
            return Result.Fail("Apenas pedidos em rascunho podem ser confirmados.");

        if (!_itens.Any())
            return Result.Fail("Pedido precisa ter ao menos um item.");

        if (Total < ValorMinimoConfirmacao)
            return Result.Fail("Valor mínimo para confirmação é R$ 10,00.");

        Status = StatusPedido.Confirmado;
        ConfirmadoEm = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result Cancelar(string motivo)
    {
        if (Status == StatusPedido.Cancelado)
            return Result.Fail("Pedido já está cancelado.");

        if (string.IsNullOrWhiteSpace(motivo))
            return Result.Fail("Motivo do cancelamento é obrigatório.");

        Status = StatusPedido.Cancelado;
        CanceladoEm = DateTime.UtcNow;
        MotivoCancelamento = motivo;
        return Result.Ok();
    }

    private void RecalcularTotal() => Total = _itens.Sum(i => i.Subtotal);
}
