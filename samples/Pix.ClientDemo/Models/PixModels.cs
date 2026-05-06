using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pix.ClientDemo.Models;

public sealed record OAuthTokenRequest(
    [property: JsonPropertyName("client_id")] string ClientId,
    [property: JsonPropertyName("client_secret")] string ClientSecret,
    [property: JsonPropertyName("grant_type")] string GrantType);

public sealed record OAuthTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    string Scope);

public sealed record CriarCobrancaRequest(
    CalendarioRequest Calendario,
    DevedorRequest Devedor,
    RecebedorRequest Recebedor,
    ValorRequest Valor,
    string ChavePix,
    string SolicitacaoPagador,
    IReadOnlyList<SplitRequest> Split,
    IReadOnlyList<InfoAdicionalRequest> InfoAdicionais,
    Dictionary<string, JsonElement>? Metadata);

public sealed record CalendarioRequest(int Expiracao);
public sealed record DevedorRequest(string Nome, string CpfCnpj, EnderecoRequest Endereco);
public sealed record EnderecoRequest(string Logradouro, string Numero, string Cidade, string Uf, string Cep);
public sealed record RecebedorRequest(string Nome, string Ispb, string Agencia, string Conta, string TipoConta);
public sealed record ValorRequest(decimal Original, decimal Abatimento, decimal Desconto, decimal Juros, decimal Multa);
public sealed record SplitRequest(string Favorecido, string Documento, decimal Valor, int Percentual);
public sealed record InfoAdicionalRequest(string Nome, string Valor);

public sealed record CobrancaResponse(
    string Txid,
    string Status,
    DateTimeOffset CriadaEm,
    DateTimeOffset ExpiraEm,
    string Location,
    string PixCopiaECola,
    string QrCodeBase64,
    IReadOnlyList<HistoricoStatusResponse> HistoricoStatus,
    decimal ValorOriginal,
    string CorrelationId);

public sealed record HistoricoStatusResponse(DateTimeOffset DataHora, string Status, string Origem);

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

public sealed record RastreabilidadeResponse(string Txid, string EndToEndId, DateTimeOffset UltimaAtualizacao, string Origem);
