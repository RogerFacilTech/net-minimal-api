using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Catalogo.Tests.Integration;

public class RateLimitingTests : IClassFixture<RateLimitingApiFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(RateLimitingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GET_Produtos_QuandoExcedeLimit_Retorna429()
    {
        // leitura limit = 3; 4ª requisição deve ser rejeitada
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 4; i++)
            lastResponse = await _client.GetAsync("/api/v1/catalogo/produtos");

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GET_Produtos_QuandoExcedeLimit_RetornaHeaderRetryAfter()
    {
        HttpResponseMessage? rejectedResponse = null;
        for (int i = 0; i < 4; i++)
        {
            var r = await _client.GetAsync("/api/v1/catalogo/produtos");
            if ((int)r.StatusCode == 429) { rejectedResponse = r; break; }
        }

        rejectedResponse.Should().NotBeNull("deve ter recebido 429 ao exceder o limite de 3 req");
        rejectedResponse!.Headers.Should().ContainKey("Retry-After");
        int.Parse(rejectedResponse.Headers.GetValues("Retry-After").First()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task POST_Produto_AtingeTetoMaisRapidoQueLeitura()
    {
        // criacao-produto limit = 2; 3ª requisição deve ser rejeitada
        var token = await AuthHelper.ObterTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { nome = "P", preco = 1.0, estoque = 1, categoria = "Eletrônicos", descricao = "d" };

        int tooManyCount = 0;
        for (int i = 0; i < 3; i++)
        {
            var r = await _client.PostAsJsonAsync("/api/v1/catalogo/produtos", payload);
            if (r.StatusCode == HttpStatusCode.TooManyRequests) tooManyCount++;
        }

        tooManyCount.Should().BeGreaterThan(0, "criação-produto limit é 2, então a 3ª deve retornar 429");
    }
}
