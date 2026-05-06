using FacShopAPI.Catalogo.HttpClientDemo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var apiBase = args.Length > 0 ? args[0] : "https://localhost:5001";

builder.AddCatalogoClient(apiBase);

var host = builder.Build();
var client = host.Services.GetRequiredService<CatalogoHttpClient>();

Console.WriteLine($"=== Catalogo Client Demo — {apiBase} ===\n");

// Demo 1: leituras em sequência (deve passar todas)
Console.WriteLine("--- Demo 1: Leituras ---");
for (int i = 1; i <= 5; i++)
{
    try
    {
        var result = await client.GetProdutosAsync();
        Console.WriteLine($"  [{i}] OK ({result?.Length} bytes)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [{i}] ERRO: {ex.Message}");
    }
    await Task.Delay(200);
}

// Demo 2: criações em rajada (espera-se 429 após 5)
Console.WriteLine("\n--- Demo 2: Criações em rajada (429 esperado após 5) ---");
var payload = new { nome = "Produto Demo", preco = 99.90, estoque = 10, categoria = "Eletrônicos", descricao = "Demo" };
for (int i = 1; i <= 8; i++)
{
    try
    {
        var response = await client.CreateProdutoAsync(payload);
        Console.WriteLine($"  [{i}] {(int)response.StatusCode} {response.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [{i}] ERRO (após retries): {ex.Message}");
    }
    await Task.Delay(100);
}

Console.WriteLine("\nDemo concluído.");
