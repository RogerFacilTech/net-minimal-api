using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace Catalogo.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCatalogoRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }
                else
                {
                    context.HttpContext.Response.Headers.RetryAfter = "10";
                }

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"erro":"Too Many Requests","mensagem":"Limite de requisições excedido. Tente novamente em breve."}""",
                    cancellationToken);
            };

            // Política 1: leitura — fixed window, 60 req/min
            options.AddFixedWindowLimiter("leitura", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 60;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Política 2: escrita — sliding window, 20 req/min
            options.AddSlidingWindowLimiter("escrita", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 6;
                opt.PermitLimit = 20;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Política 3: criação de produto — token bucket, 5 req/min
            options.AddTokenBucketLimiter("criacao-produto", opt =>
            {
                opt.TokenLimit = 5;
                opt.TokensPerPeriod = 5;
                opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });

        return services;
    }

    /// <summary>
    /// Registra políticas de rate limiting com limites customizados — usado em testes.
    /// </summary>
    public static IServiceCollection AddCatalogoRateLimitingWithLimits(
        this IServiceCollection services,
        int leituraLimit, int escritaLimit, int criacaoProdutoLimit)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "1";
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"erro":"Too Many Requests"}""", ct);
            };

            options.AddFixedWindowLimiter("leitura", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = leituraLimit;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.AddSlidingWindowLimiter("escrita", opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 6;
                opt.PermitLimit = escritaLimit;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.AddTokenBucketLimiter("criacao-produto", opt =>
            {
                opt.TokenLimit = criacaoProdutoLimit;
                opt.TokensPerPeriod = criacaoProdutoLimit;
                opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });

        return services;
    }
}
