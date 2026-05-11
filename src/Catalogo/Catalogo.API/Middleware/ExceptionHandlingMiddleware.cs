using System.Net;
using Catalogo.API.DTOs;
using FluentValidation;

namespace FacShopAPI.Shared.Middleware;

/// <summary>
/// Middleware global para tratamento de exceções
/// Referência: Melhores-Praticas-API.md - Seção "Tratamento de Erros"
/// Captura todas as exceções não tratadas e retorna respostas padronizadas
/// </summary>
/// <summary>
/// Middleware global para tratamento de exceções.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ExceptionHandlingMiddleware"/>.
    /// </summary>
    /// <param name="next">Delegate da próxima etapa do pipeline.</param>
    /// <param name="logger">Logger para registrar exceções.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Executa o middleware para capturar exceções não tratadas.
    /// </summary>
    /// <param name="context">Contexto HTTP da requisição.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção não tratada na requisição");
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Manipula exceções e retorna resposta padronizada.
    /// </summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <param name="exception">Exceção capturada.</param>
    /// <returns>Tarefa assíncrona de escrita da resposta.</returns>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Instance = context.Request.Path
        };

        // Tratar diferentes tipos de exceções
        switch (exception)
        {
            case ValidationException validationEx:
                // Exceção de validação do FluentValidation
                // Referência: Melhores-Praticas-API.md - Seção "Validação de Dados"
                context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                response.Status = StatusCodes.Status422UnprocessableEntity;
                response.Title = "Validation Failed";
                response.Type = "https://example.com/errors/validation-error";
                response.Detail = "One or more validation errors occurred.";

                foreach (var failure in validationEx.Errors.GroupBy(e => e.PropertyName))
                {
                    response.Errors[failure.Key] = failure.Select(e => e.ErrorMessage).ToList();
                }
                break;

            case KeyNotFoundException:
                // Recurso não encontrado
                // Referência: Melhores-Praticas-API.md - Seção "HTTP Status Codes"
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Status = StatusCodes.Status404NotFound;
                response.Title = "Not Found";
                response.Type = "https://example.com/errors/not-found";
                response.Detail = exception.Message;
                break;

            case ArgumentException:
                // Argumento inválido
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Status = StatusCodes.Status400BadRequest;
                response.Title = "Bad Request";
                response.Type = "https://example.com/errors/bad-request";
                response.Detail = exception.Message;
                break;

            case UnauthorizedAccessException:
                // Acesso não autorizado
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Status = StatusCodes.Status401Unauthorized;
                response.Title = "Unauthorized";
                response.Type = "https://example.com/errors/unauthorized";
                response.Detail = "Autenticação necessária";
                break;

            default:
                // Erro genérico do servidor
                // Referência: Melhores-Praticas-API.md - Seção "Tratamento de Erros"
                // NÃO expor stack trace em produção
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Status = StatusCodes.Status500InternalServerError;
                response.Title = "Internal Server Error";
                response.Type = "https://example.com/errors/internal-server-error";
                response.Detail = "Um erro interno ocorreu ao processar sua requisição";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Extensão para registrar o middleware
/// </summary>
/// <summary>
/// Métodos de extensão para registrar o ExceptionHandlingMiddleware.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adiciona o ExceptionHandlingMiddleware ao pipeline.
    /// </summary>
    /// <param name="app">Builder da aplicação.</param>
    /// <returns>Builder da aplicação.</returns>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
