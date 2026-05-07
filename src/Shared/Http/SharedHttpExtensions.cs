using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace FacShopAPI.Shared.Http;

public static class SharedHttpExtensions
{
    public static IServiceCollection AddSharedHttpInfrastructure(
        this IServiceCollection services,
        Action<IdempotencyKeyOptions>? configureIdempotency = null)
    {
        services.AddTransient<CorrelationIdHandler>();
        services.AddTransient<RequestLoggingHandler>();

        if (configureIdempotency is not null)
        {
            services.Configure(configureIdempotency);
            services.AddTransient<IdempotencyKeyHandler>();
        }

        return services;
    }

    public static IHttpClientBuilder AddApiClientWithResilience<TClient>(
        this IServiceCollection services,
        string clientName,
        Action<ApiClientOptionsBase> configureOptions,
        bool includeIdempotencyHandler = false,
        Action<IdempotencyKeyOptions>? configureIdempotency = null)
        where TClient : class
    {
        var options = new ApiClientOptionsBase();
        configureOptions(options);

        services.AddSharedHttpInfrastructure(includeIdempotencyHandler ? configureIdempotency ?? (_ => { }) : null);

        var builder = services.AddHttpClient<TClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            }
        });

        builder
            .AddHttpMessageHandler<CorrelationIdHandler>()
            .AddHttpMessageHandler<RequestLoggingHandler>()
            .AddDefaultApiResiliencePipeline(clientName, options);

        if (includeIdempotencyHandler)
        {
            builder.AddHttpMessageHandler<IdempotencyKeyHandler>();
        }

        return builder;
    }

    public static IHttpClientBuilder AddDefaultApiResiliencePipeline(
        this IHttpClientBuilder builder,
        string pipelineName,
        ApiClientOptionsBase options)
    {
        builder.AddResilienceHandler($"{pipelineName}-pipeline", pipeline =>
        {
            pipeline.AddTimeout(TimeSpan.FromSeconds(options.AttemptTimeoutSeconds));

            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(options.BaseRetryDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = args => args.Outcome switch
                {
                    { Exception: HttpRequestException } => PredicateResult.True(),
                    { Result.StatusCode: System.Net.HttpStatusCode.TooManyRequests } => PredicateResult.True(),
                    { Result.StatusCode: System.Net.HttpStatusCode.ServiceUnavailable } => PredicateResult.True(),
                    _ => PredicateResult.False()
                }
            });

            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(options.CircuitSamplingWindowSeconds),
                MinimumThroughput = options.CircuitMinimumThroughput,
                FailureRatio = options.CircuitFailureRatio,
                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakDurationSeconds)
            });

            pipeline.AddTimeout(TimeSpan.FromSeconds(options.TotalRequestTimeoutSeconds));
        });

        return builder;
    }
}
