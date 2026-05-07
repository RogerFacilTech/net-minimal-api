using FacShopAPI.Catalogo.Endpoints.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Catalogo.Tests.Integration;

// Factory com limites baixos para testar o comportamento de rejeição (429)
public class RateLimitingApiFactory : ApiFactory
{
    protected override void AddRateLimiting(IServiceCollection services) =>
        services.AddCatalogoRateLimitingWithLimits(leituraLimit: 3, escritaLimit: 3, criacaoProdutoLimit: 2);
}
