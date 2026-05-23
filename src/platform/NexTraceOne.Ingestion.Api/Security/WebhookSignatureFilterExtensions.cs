using Microsoft.Extensions.Options;

namespace NexTraceOne.Ingestion.Api.Security;

/// <summary>
/// Extensão para <see cref="RouteHandlerBuilder"/> que adiciona validação
/// HMAC-SHA256 de assinatura de webhook a um endpoint específico.
/// </summary>
public static class WebhookSignatureFilterExtensions
{
    /// <summary>
    /// Adiciona validação de assinatura HMAC-SHA256 ao endpoint.
    /// O segredo é lido de <c>Security:WebhookSecrets:{sourceName}</c>.
    /// Se o segredo não estiver configurado, emite aviso e deixa passar (modo retrocompatível).
    /// </summary>
    public static RouteHandlerBuilder WithWebhookSignature(
        this RouteHandlerBuilder builder,
        string sourceName)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var options = context.HttpContext.RequestServices
                .GetRequiredService<IOptions<WebhookSignatureOptions>>().Value;

            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("NexTraceOne.WebhookSignature");

            return await WebhookSignatureValidator.ValidateAsync(
                context, next, sourceName, options, logger);
        });
    }
}
