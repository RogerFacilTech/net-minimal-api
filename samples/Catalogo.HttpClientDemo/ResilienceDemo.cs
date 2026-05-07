using FacShopAPI.Shared.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FacShopAPI.Catalogo.HttpClientDemo;

public static class ResilienceDemo
{
    public static IHostApplicationBuilder AddCatalogoClient(
        this IHostApplicationBuilder builder,
        string baseAddress)
    {
        builder.Services.AddApiClientWithResilience<CatalogoHttpClient>(
            clientName: "catalogo",
            configureOptions: options =>
            {
                options.BaseUrl = baseAddress;
                options.AttemptTimeoutSeconds = 5;
                options.TotalRequestTimeoutSeconds = 30;
                options.MaxRetryAttempts = 3;
                options.BaseRetryDelaySeconds = 1;
                options.CircuitSamplingWindowSeconds = 30;
                options.CircuitMinimumThroughput = 5;
                options.CircuitFailureRatio = 0.5;
                options.CircuitBreakDurationSeconds = 15;
            },
            includeIdempotencyHandler: true,
            configureIdempotency: options =>
            {
                options.PathContains = ["/api/v1/catalogo/produtos"];
            });

        return builder;
    }
}
