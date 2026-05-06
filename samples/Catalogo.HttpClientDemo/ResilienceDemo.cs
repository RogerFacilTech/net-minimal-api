using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace FacShopAPI.Catalogo.HttpClientDemo;

public static class ResilienceDemo
{
    public static IHostApplicationBuilder AddCatalogoClient(
        this IHostApplicationBuilder builder,
        string baseAddress)
    {
        builder.Services
            .AddHttpClient<CatalogoHttpClient>(client =>
            {
                client.BaseAddress = new Uri(baseAddress);
            })
            .AddResilienceHandler("catalogo-pipeline", pipeline =>
            {
                // 1. Timeout por tentativa
                pipeline.AddTimeout(TimeSpan.FromSeconds(5));

                // 2. Retry com exponential backoff — só para erros transientes e 429
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = args => args.Outcome switch
                    {
                        { Exception: HttpRequestException } => PredicateResult.True(),
                        { Result.StatusCode: System.Net.HttpStatusCode.TooManyRequests } => PredicateResult.True(),
                        { Result.StatusCode: System.Net.HttpStatusCode.ServiceUnavailable } => PredicateResult.True(),
                        _ => PredicateResult.False()
                    },
                    OnRetry = args =>
                    {
                        if (args.Outcome.Result?.Headers.RetryAfter?.Delta is { } retryAfter)
                            Console.WriteLine($"[Retry] Aguardando {retryAfter.TotalSeconds}s (Retry-After do servidor)...");
                        else
                            Console.WriteLine($"[Retry] Tentativa {args.AttemptNumber + 1}...");
                        return ValueTask.CompletedTask;
                    }
                });

                // 3. Circuit breaker — abre após 50% de falhas em 30s (mínimo 5 reqs)
                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 5,
                    FailureRatio = 0.5,
                    BreakDuration = TimeSpan.FromSeconds(15),
                    OnOpened = args =>
                    {
                        Console.WriteLine($"[CircuitBreaker] Aberto por {args.BreakDuration.TotalSeconds}s");
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = _ =>
                    {
                        Console.WriteLine("[CircuitBreaker] Fechado — serviço recuperado");
                        return ValueTask.CompletedTask;
                    }
                });

                // 4. Timeout global (cobre toda a operação incluindo retries)
                pipeline.AddTimeout(TimeSpan.FromSeconds(30));
            });

        return builder;
    }
}
