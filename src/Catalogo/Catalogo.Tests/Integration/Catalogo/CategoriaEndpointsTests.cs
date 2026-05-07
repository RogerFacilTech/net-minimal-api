using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Catalogo.Tests.Integration;
using FacShopAPI.Catalogo.Application.DTOs.Categoria;
using FluentAssertions;
using Xunit;

namespace Catalogo.Tests.Integration.Catalogo;

public class CategoriaEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CategoriaEndpointsTests(ApiFactory factory) => _factory = factory;

    private HttpClient CriarCliente() => _factory.CreateClient();

    private async Task<HttpClient> CriarClienteAutenticadoAsync()
    {
        var client = _factory.CreateClient();
        var token = await AuthHelper.ObterTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task GET_Categorias_SemAutenticacao_Retorna200()
    {
        var client = CriarCliente();

        var response = await client.GetAsync("/api/v1/catalogo/categorias");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CategoriaResponse>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_CriarCategoria_Valida_Retorna201()
    {
        var client = await CriarClienteAutenticadoAsync();

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Eletrônicos Teste" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CategoriaResponse>();
        result!.Nome.Should().Be("Eletrônicos Teste");
        result.Slug.Should().Be("eletronicos-teste");
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_CriarSubcategoria_ComPaiExistente_Retorna201()
    {
        var client = await CriarClienteAutenticadoAsync();

        var paiResponse = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Categoria Pai Sub" });
        var pai = await paiResponse.Content.ReadFromJsonAsync<CategoriaResponse>();

        var subResponse = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Subcategoria Sub", CategoriaPaiId = pai!.Id });

        subResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sub = await subResponse.Content.ReadFromJsonAsync<CategoriaResponse>();
        sub!.CategoriaPaiId.Should().Be(pai.Id);
    }

    [Fact]
    public async Task POST_CriarSubcategoriaDeSubcategoria_Retorna422()
    {
        var client = await CriarClienteAutenticadoAsync();

        var paiResp = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Pai Max Nivel" });
        var pai = await paiResp.Content.ReadFromJsonAsync<CategoriaResponse>();

        var filhaResp = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Filha Nivel", CategoriaPaiId = pai!.Id });
        var filha = await filhaResp.Content.ReadFromJsonAsync<CategoriaResponse>();

        var netoResp = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Neto Nivel", CategoriaPaiId = filha!.Id });

        netoResp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_CriarCategoria_NomeVazio_Retorna422()
    {
        var client = await CriarClienteAutenticadoAsync();

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DELETE_DesativarCategoria_Existente_Retorna204()
    {
        var client = await CriarClienteAutenticadoAsync();

        var criarResp = await client.PostAsJsonAsync("/api/v1/catalogo/categorias",
            new CriarCategoriaRequest { Nome = "Para Deletar" });
        var cat = await criarResp.Content.ReadFromJsonAsync<CategoriaResponse>();

        var deleteResp = await client.DeleteAsync($"/api/v1/catalogo/categorias/{cat!.Id}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_ObterCategoria_Inexistente_Retorna404()
    {
        var client = CriarCliente();

        var response = await client.GetAsync("/api/v1/catalogo/categorias/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
