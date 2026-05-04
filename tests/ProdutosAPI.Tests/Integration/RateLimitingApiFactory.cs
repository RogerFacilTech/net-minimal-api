using Microsoft.Extensions.DependencyInjection;
using ProdutosAPI.Catalogo.Endpoints.Extensions;

namespace ProdutosAPI.Tests.Integration;

// Factory com limites baixos para testar o comportamento de rejeição (429)
public class RateLimitingApiFactory : ApiFactory
{
    protected override void AddRateLimiting(IServiceCollection services) =>
        services.AddCatalogoRateLimitingWithLimits(leituraLimit: 3, escritaLimit: 3, criacaoProdutoLimit: 2);
}
