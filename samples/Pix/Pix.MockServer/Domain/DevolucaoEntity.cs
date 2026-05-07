using Pix.MockServer.Contracts;

namespace Pix.MockServer.Domain;

public sealed class DevolucaoEntity
{
    public required string DevolucaoId { get; init; }
    public required CriarDevolucaoRequest Request { get; init; }
    public required DateTimeOffset SolicitadaEm { get; init; }
    public required DateTimeOffset? LiquidadaEm { get; set; }
    public required string Status { get; set; }
    public required string CorrelationId { get; init; }
    public required string OrigemAtualizacao { get; set; }
}
