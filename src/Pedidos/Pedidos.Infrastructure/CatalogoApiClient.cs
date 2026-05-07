using System.Net;
using System.Net.Http.Json;
using FacShopAPI.Pedidos.Domain;

namespace FacShopAPI.Pedidos.Infrastructure;

public interface ICatalogoApiClient
{
    Task<ProdutoSnapshot?> ObterProdutoSnapshotAsync(int produtoId, CancellationToken ct = default);
}

public sealed class CatalogoApiClient(HttpClient httpClient) : ICatalogoApiClient
{
    public async Task<ProdutoSnapshot?> ObterProdutoSnapshotAsync(int produtoId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/v1/catalogo/produtos/{produtoId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var produto = await response.Content.ReadFromJsonAsync<CatalogoProdutoResponse>(cancellationToken: ct);
        if (produto is null)
        {
            return null;
        }

        return new ProdutoSnapshot(produto.Id, produto.Nome, produto.Preco);
    }

    private sealed class CatalogoProdutoResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
    }
}
