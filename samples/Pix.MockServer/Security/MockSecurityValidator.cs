using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;

namespace Pix.MockServer.Security;

public interface IMockSecurityValidator
{
    bool TryValidate(HttpContext context, out IResult? failureResult, out string correlationId);
}

public sealed class MockSecurityValidator(
    ITokenService tokenService,
    PixMtlsCertificateBundle mtlsBundle,
    IHostEnvironment hostEnvironment) : IMockSecurityValidator
{
    private const string TestingMtlsHeaderName = "X-MTLS-Client-Cert";

    public bool TryValidate(HttpContext context, out IResult? failureResult, out string correlationId)
    {
        correlationId = EnsureCorrelationId(context);

        if (!HasValidMutualTls(context))
        {
            failureResult = Results.Problem(
                title: "mTLS obrigatório",
                detail: "Certificado de cliente TLS não encontrado ou inválido.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: BuildExtensions(context, correlationId));
            return false;
        }

        var authorization = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            failureResult = Results.Problem(
                title: "Token ausente",
                detail: "Forneça um token Bearer válido.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: BuildExtensions(context, correlationId));
            return false;
        }

        var token = authorization["Bearer ".Length..].Trim();
        if (!tokenService.ValidateToken(token))
        {
            failureResult = Results.Problem(
                title: "Token inválido",
                detail: "O token informado está inválido ou expirado.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: BuildExtensions(context, correlationId));
            return false;
        }

        failureResult = null;
        return true;
    }

    private bool HasValidMutualTls(HttpContext context)
    {
        if (context.Connection.ClientCertificate is X509Certificate2 clientCertificate)
        {
            return PixMtlsCertificateStore.ValidateClientCertificate(
                clientCertificate,
                mtlsBundle.CaCertificatePath,
                mtlsBundle.ExpectedClientSubject);
        }

        // Fallback apenas para ambiente de testes com TestServer (sem handshake TLS real).
        if (hostEnvironment.IsEnvironment("Testing"))
        {
            return context.Request.Headers.TryGetValue(TestingMtlsHeaderName, out var value)
                && !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }

    private static string EnsureCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].ToString();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        correlationId = Guid.NewGuid().ToString("N");
        context.Request.Headers["X-Correlation-Id"] = correlationId;
        return correlationId;
    }

    private static Dictionary<string, object?> BuildExtensions(HttpContext context, string correlationId)
        => new()
        {
            ["traceId"] = context.TraceIdentifier,
            ["correlationId"] = correlationId
        };
}
