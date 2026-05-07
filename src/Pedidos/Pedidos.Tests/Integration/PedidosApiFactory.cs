using Xunit;

namespace Pedidos.Tests.Integration;

/// <summary>
/// Factory para testes de integração de Pedidos
/// Configura aplicação para testes HTTP sem mocks
/// </summary>
public class PedidosApiFactory : IAsyncLifetime
{
    // Futura implementação com WebApplicationFactory
    // private readonly string _connectionString = "Data Source=:memory:;";

    public async Task InitializeAsync()
    {
        // Inicializar banco de testes
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Limpar recursos após testes
        await Task.CompletedTask;
    }

    public HttpClient CreateClient()
    {
        // Retornar cliente HTTP para testes
        return new HttpClient();
    }
}
