using System.Security.Cryptography;
using System.Text;

namespace NexTraceOne.Ingestion.Api.Security;

/// <summary>
/// Validador de assinatura HMAC-SHA256 para webhooks recebidos.
///
/// Suporta dois formatos de cabeçalho:
/// - GitHub-style: X-Hub-Signature-256: sha256=&lt;lowercase-hex&gt;
/// - SonarQube-style: X-Sonar-Webhook-HMAC-SHA256: &lt;lowercase-hex&gt;
///
/// Segurança:
/// - Comparação sempre em tempo constante (<see cref="CryptographicOperations.FixedTimeEquals"/>)
///   para prevenir timing attacks.
/// - O corpo do request é lido e armazenado em buffer antes de chamar o handler,
///   permitindo que o model binding funcione normalmente a seguir.
/// - Se nenhum segredo estiver configurado para a fonte, emite aviso e deixa passar
///   (modo de compatibilidade retroactiva). Em produção recomenda-se configurar sempre.
/// </summary>
public static class WebhookSignatureValidator
{
    internal const string GitHubHeader = "X-Hub-Signature-256";
    internal const string SonarHeader = "X-Sonar-Webhook-HMAC-SHA256";
    private const string GitHubPrefix = "sha256=";

    /// <summary>
    /// Valida cabeçalho e corpo de forma pura — sem dependência de HttpContext.
    /// Usado directamente em testes unitários e pelo middleware ASP.NET Core.
    /// </summary>
    /// <returns>
    /// <c>(hasSignature: false, isValid: false)</c> — nenhum cabeçalho de assinatura presente.<br/>
    /// <c>(hasSignature: true, isValid: false)</c> — cabeçalho presente mas assinatura inválida.<br/>
    /// <c>(hasSignature: true, isValid: true)</c> — assinatura válida.
    /// </returns>
    public static (bool HasSignature, bool IsValid) Validate(
        IHeaderDictionary headers, byte[] body, string secret)
    {
        if (!TryExtractSignature(headers, out var receivedHex))
            return (HasSignature: false, IsValid: false);

        return (HasSignature: true, IsValid: VerifyHmac(body, secret, receivedHex));
    }

    /// <summary>
    /// Valida a assinatura HMAC-SHA256 do request e chama o próximo handler se válida.
    /// Usado como delegate em <see cref="IEndpointFilter"/> via <c>AddEndpointFilter</c>.
    /// </summary>
    public static async ValueTask<object?> ValidateAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next,
        string sourceName,
        WebhookSignatureOptions options,
        ILogger logger)
    {
        var httpContext = context.HttpContext;

        if (!options.TryGetSecret(sourceName, out var secret))
        {
            logger.LogWarning(
                "Webhook signature validation SKIPPED for source '{Source}': " +
                "no secret configured at Security:WebhookSecrets:{Source}. " +
                "Any caller with a valid API key can push data without proof-of-origin. " +
                "Set the secret via environment variable Security__WebhookSecrets__{Source}.",
                sourceName, sourceName, sourceName);
            return await next(context);
        }

        // Bufferar o body para que possa ser lido pelo model binding depois desta validação.
        httpContext.Request.EnableBuffering();

        byte[] bodyBytes;
        using (var ms = new MemoryStream())
        {
            await httpContext.Request.Body.CopyToAsync(ms);
            bodyBytes = ms.ToArray();
        }

        // Rebobinar para que o model binding consiga ler o body normalmente.
        httpContext.Request.Body.Position = 0;

        var (hasSignature, isValid) = Validate(httpContext.Request.Headers, bodyBytes, secret);

        if (!hasSignature)
        {
            logger.LogWarning(
                "Webhook signature header missing for source '{Source}' from {RemoteIp}. " +
                "Expected: {GitHubHeader} or {SonarHeader}.",
                sourceName,
                httpContext.Connection.RemoteIpAddress,
                GitHubHeader,
                SonarHeader);

            return Results.Json(
                new
                {
                    title = "Webhook Signature Required",
                    status = StatusCodes.Status401Unauthorized,
                    code = "webhook_signature_missing",
                    detail = $"Include {GitHubHeader} (sha256=<hex>) or {SonarHeader} (<hex>) with the request."
                },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!isValid)
        {
            logger.LogWarning(
                "Webhook signature MISMATCH for source '{Source}' from {RemoteIp}. Payload rejected.",
                sourceName,
                httpContext.Connection.RemoteIpAddress);

            return Results.Json(
                new
                {
                    title = "Webhook Signature Invalid",
                    status = StatusCodes.Status401Unauthorized,
                    code = "webhook_signature_invalid",
                    detail = "The provided HMAC-SHA256 signature does not match the expected value."
                },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }

    /// <summary>
    /// Tenta extrair a assinatura hex do cabeçalho do request.
    /// Suporta formato GitHub (sha256=hex) e SonarQube (hex directo).
    /// </summary>
    internal static bool TryExtractSignature(IHeaderDictionary headers, out string receivedHex)
    {
        if (headers.TryGetValue(GitHubHeader, out var gitHubSig))
        {
            var sig = gitHubSig.ToString();
            if (sig.StartsWith(GitHubPrefix, StringComparison.OrdinalIgnoreCase))
            {
                receivedHex = sig[GitHubPrefix.Length..];
                return !string.IsNullOrWhiteSpace(receivedHex);
            }
        }

        if (headers.TryGetValue(SonarHeader, out var sonarSig))
        {
            receivedHex = sonarSig.ToString();
            return !string.IsNullOrWhiteSpace(receivedHex);
        }

        receivedHex = string.Empty;
        return false;
    }

    /// <summary>
    /// Verifica a assinatura HMAC-SHA256 em tempo constante.
    /// </summary>
    internal static bool VerifyHmac(byte[] body, string secret, string receivedHex)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var computedHash = HMACSHA256.HashData(secretBytes, body);
        var computedHex = Convert.ToHexString(computedHash).ToLowerInvariant();
        var receivedNormalized = receivedHex.Trim().ToLowerInvariant();

        if (computedHex.Length != receivedNormalized.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(computedHex),
            Encoding.ASCII.GetBytes(receivedNormalized));
    }
}
