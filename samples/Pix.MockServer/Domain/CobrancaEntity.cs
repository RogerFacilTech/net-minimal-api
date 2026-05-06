using Pix.MockServer.Contracts;

namespace Pix.MockServer.Domain;

public sealed record HistoricoStatusItem(DateTimeOffset DataHora, string Status, string Origem);

public sealed class CobrancaEntity
{
    public required string Txid { get; init; }
    public required CriarCobrancaRequest Request { get; init; }
    public required DateTimeOffset CriadaEm { get; init; }
    public required DateTimeOffset ExpiraEm { get; init; }
    public required string CorrelationId { get; init; }
    public required string Status { get; set; }
    public required List<HistoricoStatusItem> HistoricoStatus { get; init; }
}
