using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProdutosAPI.Shared.Middleware;

/// <summary>
/// Middleware para garantir Idempotência em requisições críticas (POST, PUT, PATCH).
/// Evita que a mesma ação não-segura seja processada duas vezes em caso de tentar novamente
/// uma requisição por falhas na rede.
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private const string IdempotencyHeader = "Idempotency-Key";

    public IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Se a requisição já é idempotente por natureza (GET, HEAD, OPTIONS)
        // Ignoramos a checagem de chave.
        if (HttpMethods.IsGet(context.Request.Method) || 
            HttpMethods.IsHead(context.Request.Method) || 
            HttpMethods.IsOptions(context.Request.Method) || 
            HttpMethods.IsDelete(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Tenta obter a chave de idempotência do cabeçalho
        if (!context.Request.Headers.TryGetValue(IdempotencyHeader, out var idempotencyKey) || 
            string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // Se o header é obrigatório, poderíamos retornar 400 Bad Request aqui.
            // Para efeitos educativos, deixaremos passar caso não seja informada.
            await _next(context);
            return;
        }

        var keyString = idempotencyKey.ToString();
        var cacheKey = $"Idempotency_{keyString}";

        // Verifica se já processamos esta chave recentemente
        if (_cache.TryGetValue(cacheKey, out CachedResponse? cachedResponse))
        {
            // Se já processamos, apenas devolvemos a mesma resposta do cache (curto-circuito)
            context.Response.StatusCode = cachedResponse!.StatusCode;
            context.Response.ContentType = cachedResponse.ContentType;
            
            if (cachedResponse.Body != null)
            {
                await context.Response.WriteAsync(cachedResponse.Body);
            }

            return;
        }

        // Interceptar o stream de resposta para salvarmos no cache depois
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            // Prossegue o processamento real da API
            await _next(context);

            // Se for sucesso (2xx), gravamos a resposta no cache por 24 horas
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var bodyText = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);

                var responseToCache = new CachedResponse
                {
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType ?? "application/json",
                    Body = bodyText
                };

                _cache.Set(cacheKey, responseToCache, TimeSpan.FromHours(24));
            }

            // Copiar do nosso stream para o stream original real do cliente
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}

// Classe auxiliar para armazenar respostas em cache
public class CachedResponse
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

// Extension Method para injetar no Program.cs
public static class IdempotencyMiddlewareExtensions
{
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IdempotencyMiddleware>();
    }
}
