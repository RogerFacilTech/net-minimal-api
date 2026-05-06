using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Catalogo.Tests.Integration;
using FacShopAPI.Catalogo.Application.DTOs.Common;
using FacShopAPI.Catalogo.Application.DTOs.Produto;
using FluentAssertions;
using Xunit;

namespace Catalogo.Tests.Endpoints;

/// <summary>
/// Testes de integração para os endpoints de Produto.
/// Verifica status HTTP, payload e headers reais via WebApplicationFactory.
/// </summary>
public class ProdutoEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ProdutoEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CriarCliente() => _factory.CreateClient();

    private async Task<HttpClient> CriarClienteAutenticadoAsync()
    {
        var client = _factory.CreateClient();
        var token = await AuthHelper.ObterTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static CriarProdutoRequest RequestValido(string nome = "Produto de Teste") => new()
    {
        Nome = nome,
        Descricao = "Descrição detalhada do produto de teste",
        Preco = 299.90m,
        Categoria = "Eletrônicos",
        Estoque = 20,
        ContatoEmail = "vendas@teste.com"
    };

    #region GET /api/v1/catalogo/produtos

    [Fact]
    public async Task GET_Produtos_SemFiltros_Retorna200ComListaPaginada()
    {
        var client = CriarCliente();

        var response = await client.GetAsync("/api/v1/catalogo/produtos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<ProdutoResponse>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeEmpty();
        result.Pagination.TotalItems.Should().BeGreaterThan(0);
        result.Pagination.Page.Should().Be(1);
    }

    [Fact]
    public async Task GET_Produtos_ComFiltroCategoria_Retorna200ApenasComCategoriaSolicitada()
    {
        var client = CriarCliente();

        var response = await client.GetAsync("/api/v1/catalogo/produtos?categoria=Livros");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<ProdutoResponse>>();
        result.Should().NotBeNull();
        result!.Data.Should().AllSatisfy(p => p.Categoria.Should().Be("Livros"));
    }

    [Fact]
    public async Task GET_Produtos_NaoRequerAutenticacao()
    {
        // endpoint marcado como AllowAnonymous
        var client = CriarCliente();

        var response = await client.GetAsync("/api/v1/catalogo/produtos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GET /api/v1/catalogo/produtos/{id}

    [Fact]
    public async Task GET_Produto_ComIdExistente_Retorna200ComDados()
    {
        var client = CriarCliente();

        // ID 1 é inserido pelo DbSeeder
        var response = await client.GetAsync("/api/v1/catalogo/produtos/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var produto = await response.Content.ReadFromJsonAsync<ProdutoResponse>();
        produto.Should().NotBeNull();
        produto!.Id.Should().Be(1);
        produto.Nome.Should().NotBeNullOrEmpty();
        produto.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task GET_Produto_ComIdInexistente_Retorna404()
    {
        var client = CriarCliente();

        var response = await client.GetAsync("/api/v1/catalogo/produtos/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Produto_NaoRequerAutenticacao()
    {
        var client = CriarCliente();

        var response = await client.GetAsync("/api/v1/catalogo/produtos/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region POST /api/v1/catalogo/produtos

    [Fact]
    public async Task POST_Produto_SemAutenticacao_Retorna401()
    {
        var client = CriarCliente();

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", RequestValido());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Produto_ComDadosValidos_Retorna201ComLocationHeader()
    {
        var client = await CriarClienteAutenticadoAsync();

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", RequestValido("Mouse Gamer Pro"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var produto = await response.Content.ReadFromJsonAsync<ProdutoResponse>();
        produto.Should().NotBeNull();
        produto!.Nome.Should().Be("Mouse Gamer Pro");
        produto.Preco.Should().Be(299.90m);
        produto.Ativo.Should().BeTrue();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/v1/catalogo/produtos/{produto.Id}");
    }

    [Fact]
    public async Task POST_Produto_ComNomeVazio_Retorna422()
    {
        var client = await CriarClienteAutenticadoAsync();
        var request = new CriarProdutoRequest
        {
            Nome = "",
            Descricao = "Descrição de teste",
            Preco = 100m,
            Categoria = "Eletrônicos",
            Estoque = 10,
            ContatoEmail = "teste@teste.com"
        };

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_Produto_ComPrecoZero_Retorna422()
    {
        var client = await CriarClienteAutenticadoAsync();
        var request = new CriarProdutoRequest
        {
            Nome = "Produto Valido",
            Descricao = "Descrição de teste",
            Preco = 0m,
            Categoria = "Eletrônicos",
            Estoque = 10,
            ContatoEmail = "teste@teste.com"
        };

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_Produto_ComCategoriaInvalida_Retorna422()
    {
        var client = await CriarClienteAutenticadoAsync();
        var request = new CriarProdutoRequest
        {
            Nome = "Produto Valido",
            Descricao = "Descrição de teste",
            Preco = 100m,
            Categoria = "CategoriaInexistente",
            Estoque = 10,
            ContatoEmail = "teste@teste.com"
        };

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task POST_Produto_ComEmailInvalido_Retorna422()
    {
        var client = await CriarClienteAutenticadoAsync();
        var request = new CriarProdutoRequest
        {
            Nome = "Produto Valido",
            Descricao = "Descrição de teste",
            Preco = 100m,
            Categoria = "Eletrônicos",
            Estoque = 10,
            ContatoEmail = "nao-e-um-email"
        };

        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    #endregion

    #region PUT /api/v1/catalogo/produtos/{id}

    [Fact]
    public async Task PUT_Produto_SemAutenticacao_Retorna401()
    {
        var client = CriarCliente();

        var response = await client.PutAsJsonAsync("/api/v1/catalogo/produtos/1", RequestValido());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_Produto_ComDadosValidos_Retorna200ComDadosAtualizados()
    {
        var client = await CriarClienteAutenticadoAsync();

        // Criar produto para atualizar
        var criado = await CriarProdutoAsync(client, "Produto para PUT");

        var requestAtualizado = new CriarProdutoRequest
        {
            Nome = "Produto Atualizado via PUT",
            Descricao = "Descrição atualizada",
            Preco = 499.99m,
            Categoria = "Eletrônicos",
            Estoque = 5,
            ContatoEmail = "atualizado@teste.com"
        };

        var response = await client.PutAsJsonAsync($"/api/v1/catalogo/produtos/{criado.Id}", requestAtualizado);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var produto = await response.Content.ReadFromJsonAsync<ProdutoResponse>();
        produto.Should().NotBeNull();
        produto!.Nome.Should().Be("Produto Atualizado via PUT");
        produto.Preco.Should().Be(499.99m);
        produto.Estoque.Should().Be(5);
    }

    [Fact]
    public async Task PUT_Produto_ComIdInexistente_Retorna404()
    {
        var client = await CriarClienteAutenticadoAsync();

        var response = await client.PutAsJsonAsync("/api/v1/catalogo/produtos/99999", RequestValido());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_Produto_ComDadosInvalidos_Retorna422()
    {
        var client = await CriarClienteAutenticadoAsync();
        var criado = await CriarProdutoAsync(client, "Produto para PUT invalido");

        var requestInvalido = new CriarProdutoRequest
        {
            Nome = "",
            Descricao = "Descrição de teste",
            Preco = 100m,
            Categoria = "Eletrônicos",
            Estoque = 10,
            ContatoEmail = "teste@teste.com"
        };

        var response = await client.PutAsJsonAsync($"/api/v1/catalogo/produtos/{criado.Id}", requestInvalido);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    #endregion

    #region PATCH /api/v1/catalogo/produtos/{id}

    [Fact]
    public async Task PATCH_Produto_SemAutenticacao_Retorna401()
    {
        var client = CriarCliente();
        var request = new AtualizarProdutoRequest { Nome = "Novo Nome" };

        var response = await client.PatchAsJsonAsync("/api/v1/catalogo/produtos/1", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PATCH_Produto_ComNome_Retorna200AtualizandoApenasCampoFornecido()
    {
        var client = await CriarClienteAutenticadoAsync();
        var criado = await CriarProdutoAsync(client, "Produto Original PATCH");

        var request = new AtualizarProdutoRequest { Nome = "Nome Atualizado Parcialmente" };

        var response = await client.PatchAsJsonAsync($"/api/v1/catalogo/produtos/{criado.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var produto = await response.Content.ReadFromJsonAsync<ProdutoResponse>();
        produto.Should().NotBeNull();
        produto!.Nome.Should().Be("Nome Atualizado Parcialmente");
        produto.Preco.Should().Be(criado.Preco); // preço não foi alterado
    }

    [Fact]
    public async Task PATCH_Produto_ComIdInexistente_Retorna404()
    {
        var client = await CriarClienteAutenticadoAsync();
        var request = new AtualizarProdutoRequest { Nome = "Qualquer" };

        var response = await client.PatchAsJsonAsync("/api/v1/catalogo/produtos/99999", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/v1/catalogo/produtos/{id}

    [Fact]
    public async Task DELETE_Produto_SemAutenticacao_Retorna401()
    {
        var client = CriarCliente();

        var response = await client.DeleteAsync("/api/v1/catalogo/produtos/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_Produto_ComIdExistente_Retorna204()
    {
        var client = await CriarClienteAutenticadoAsync();
        var criado = await CriarProdutoAsync(client, "Produto para Deletar");

        var response = await client.DeleteAsync($"/api/v1/catalogo/produtos/{criado.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_Produto_AposaDelecao_ProdutoNaoEhMaisAcessivel()
    {
        var client = await CriarClienteAutenticadoAsync();
        var criado = await CriarProdutoAsync(client, "Produto Soft Delete");

        await client.DeleteAsync($"/api/v1/catalogo/produtos/{criado.Id}");

        // Soft delete: produto deve retornar 404 após deletado
        var getResponse = await client.GetAsync($"/api/v1/catalogo/produtos/{criado.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Produto_ComIdInexistente_Retorna404()
    {
        var client = await CriarClienteAutenticadoAsync();

        var response = await client.DeleteAsync("/api/v1/catalogo/produtos/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    // Helper para criar produto e retornar o response
    private static async Task<ProdutoResponse> CriarProdutoAsync(HttpClient client, string nome)
    {
        var request = new CriarProdutoRequest
        {
            Nome = nome,
            Descricao = "Descrição gerada para teste",
            Preco = 150m,
            Categoria = "Outros",
            Estoque = 10,
            ContatoEmail = "helper@teste.com"
        };
        var response = await client.PostAsJsonAsync("/api/v1/catalogo/produtos", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProdutoResponse>())!;
    }
}
