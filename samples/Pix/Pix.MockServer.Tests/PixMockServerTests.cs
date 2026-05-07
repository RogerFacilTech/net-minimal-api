using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Pix.MockServer.Contracts;
using Xunit;

namespace Pix.MockServer.Tests;

public sealed class PixMockServerTests(PixMockServerFactory factory) : IClassFixture<PixMockServerFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CriarCobranca_Valida_DeveRetornar201()
    {
        await AuthenticateAsync();

        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var response = await _client.PostAsJsonAsync("/pix/v1/cobrancas", BuildCobrancaRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var cobranca = await response.Content.ReadFromJsonAsync<CobrancaResponse>();
        cobranca.Should().NotBeNull();
        cobranca!.Status.Should().Be("ATIVA");
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CriarCobranca_Invalida_DeveRetornar400Problem()
    {
        await AuthenticateAsync();

        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var invalida = BuildCobrancaRequest() with
        {
            Calendario = new CalendarioRequest(10),
            Devedor = new DevedorRequest("", "abc", BuildCobrancaRequest().Devedor.Endereco)
        };

        var response = await _client.PostAsJsonAsync("/pix/v1/cobrancas", invalida);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Idempotencia_MesmaChaveMesmoPayload_DeveRetornarMesmaCobranca()
    {
        await AuthenticateAsync();

        var idempotency = Guid.NewGuid().ToString("N");
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotency);

        var request = BuildCobrancaRequest();
        var first = await _client.PostAsJsonAsync("/pix/v1/cobrancas", request);
        var firstBody = await first.Content.ReadFromJsonAsync<CobrancaResponse>();

        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", idempotency);
        var second = await _client.PostAsJsonAsync("/pix/v1/cobrancas", request);
        var secondBody = await second.Content.ReadFromJsonAsync<CobrancaResponse>();

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        secondBody!.Txid.Should().Be(firstBody!.Txid);
    }

    [Fact]
    public async Task Idempotencia_MesmaChavePayloadDiferente_DeveRetornar409()
    {
        await AuthenticateAsync();

        var key = Guid.NewGuid().ToString("N");
        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", key);

        var first = await _client.PostAsJsonAsync("/pix/v1/cobrancas", BuildCobrancaRequest());
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var changed = BuildCobrancaRequest() with
        {
            Valor = new ValorRequest(999m, 0m, 0m, 0m, 0m)
        };

        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", key);
        var second = await _client.PostAsJsonAsync("/pix/v1/cobrancas", changed);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SimularLiquidacao_DeveAtualizarStatusParaConcluida()
    {
        await AuthenticateAsync();

        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var create = await _client.PostAsJsonAsync("/pix/v1/cobrancas", BuildCobrancaRequest());
        var created = await create.Content.ReadFromJsonAsync<CobrancaResponse>();

        var liquidar = await _client.PostAsync($"/pix/v1/cobrancas/{created!.Txid}/simular-liquidacao", null);
        var liquidada = await liquidar.Content.ReadFromJsonAsync<CobrancaResponse>();

        liquidar.StatusCode.Should().Be(HttpStatusCode.OK);
        liquidada!.Status.Should().Be("CONCLUIDA");
    }

    [Fact]
    public async Task Devolucao_AposLiquidacao_DeveCompletarFluxo()
    {
        await AuthenticateAsync();

        _client.DefaultRequestHeaders.Remove("Idempotency-Key");
        _client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var create = await _client.PostAsJsonAsync("/pix/v1/cobrancas", BuildCobrancaRequest());
        var created = await create.Content.ReadFromJsonAsync<CobrancaResponse>();
        await _client.PostAsync($"/pix/v1/cobrancas/{created!.Txid}/simular-liquidacao", null);

        var devolucaoRequest = new CriarDevolucaoRequest(
            created.Txid,
            $"E2E{Guid.NewGuid():N}"[..20],
            10m,
            "ORIGINAL",
            "Teste devolucao",
            new Dictionary<string, JsonElement>());

        var response = await _client.PostAsJsonAsync("/pix/v1/devolucoes", devolucaoRequest);
        var devolucao = await response.Content.ReadFromJsonAsync<DevolucaoResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        devolucao!.Status.Should().Be("DEVOLVIDA");

        var consulta = await _client.GetAsync($"/pix/v1/devolucoes/{devolucao.DevolucaoId}");
        consulta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SemTokenOuSemMtls_DeveRetornar401e403()
    {
        var semToken = factory.CreateClient();
        semToken.DefaultRequestHeaders.Add("X-MTLS-Client-Cert", "demo");
        semToken.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var unauthorized = await semToken.PostAsJsonAsync("/pix/v1/cobrancas", BuildCobrancaRequest());
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var semMtls = factory.CreateClient();
        var token = await GetTokenAsync(semMtls);
        semMtls.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        semMtls.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var forbidden = await semMtls.PostAsJsonAsync("/pix/v1/cobrancas", BuildCobrancaRequest());
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task AuthenticateAsync()
    {
        _client.DefaultRequestHeaders.Remove("X-MTLS-Client-Cert");
        _client.DefaultRequestHeaders.Add("X-MTLS-Client-Cert", "demo-certificado-cliente");

        var token = await GetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/oauth/token",
            new OAuthTokenRequest("pix-demo-client", "pix-demo-secret", "client_credentials"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>();
        return body!.AccessToken;
    }

    private static CriarCobrancaRequest BuildCobrancaRequest()
        => new(
            new CalendarioRequest(3600),
            new DevedorRequest(
                "Cliente Exemplo",
                "12345678901",
                new EnderecoRequest("Rua A", "100", "Sao Paulo", "SP", "01001000")),
            new RecebedorRequest("Empresa", "12345678", "0001", "99999", "CACC"),
            new ValorRequest(100m, 0m, 0m, 0m, 0m),
            "chave-pix-demo",
            "Pagamento demo",
            [new SplitRequest("Parceiro", "11222333000181", 10m, 10)],
            [new InfoAdicionalRequest("pedido", "123")],
            new Dictionary<string, JsonElement>());
}
