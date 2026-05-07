using System.Text.Json;

namespace Pix.MockServer.Contracts;

public sealed record CriarDevolucaoRequest(
    string Txid,
    string EndToEndId,
    decimal Valor,
    string Natureza,
    string Motivo,
    Dictionary<string, JsonElement>? Metadata);

public sealed record DevolucaoResponse(
    string DevolucaoId,
    string Status,
    DateTimeOffset SolicitadaEm,
    DateTimeOffset? LiquidadaEm,
    string Motivo,
    RastreabilidadeResponse Rastreabilidade,
    string CorrelationId);

public sealed record RastreabilidadeResponse(
    string Txid,
    string EndToEndId,
    DateTimeOffset UltimaAtualizacao,
    string Origem);
