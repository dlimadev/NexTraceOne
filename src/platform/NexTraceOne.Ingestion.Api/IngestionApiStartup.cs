using NexTraceOne.BuildingBlocks.Security.Authentication;
using NexTraceOne.Ingestion.Api.Security;

namespace NexTraceOne.Ingestion.Api;

/// <summary>
/// Extensões de arranque para a Ingestion API.
/// Valida a configuração de segurança antes do host aceitar tráfego.
/// </summary>
internal static class IngestionApiStartup
{
    /// <summary>
    /// Valida que existe pelo menos uma API Key com tenant e permissão <c>integrations:write</c>
    /// ou <c>integrations:read</c> configurada.
    /// Em Development: regista um aviso e continua se não houver chaves válidas.
    /// Em produção: lança <see cref="InvalidOperationException"/> impedindo o arranque.
    /// </summary>
    internal static void ValidateIngestionSecurityConfiguration(this WebApplication app)
    {
        var configuredKeys = app.Configuration
            .GetSection("Security:ApiKeys")
            .Get<List<ApiKeyConfiguration>>() ?? [];

        var validWriteKeys = configuredKeys.Where(IsValidIngestionWriteKey).ToList();
        var validReadKeys = configuredKeys.Where(IsValidIngestionReadKey).ToList();

        if (validWriteKeys.Count == 0 && validReadKeys.Count == 0)
        {
            const string message =
                "Ingestion.Api requires at least one API key with a valid tenant and " +
                "'integrations:write' or 'integrations:read' permission configured under 'Security:ApiKeys'.";

            if (app.Environment.IsDevelopment())
            {
                app.Logger.LogWarning(
                    "{Message} Requests will be rejected until a valid API key is configured " +
                    "via appsettings, secrets or environment variables.",
                    message);
                return;
            }

            throw new InvalidOperationException(message);
        }

        app.Logger.LogInformation(
            "Ingestion.Api security initialized — write keys: {WriteCount}, read keys: {ReadCount}. Client(s): {ClientIds}",
            validWriteKeys.Count,
            validReadKeys.Count,
            string.Join(", ", configuredKeys
                .Where(k => IsValidIngestionWriteKey(k) || IsValidIngestionReadKey(k))
                .Select(k => k.ClientId)));
    }

    private static bool IsValidIngestionWriteKey(ApiKeyConfiguration apiKey)
        => IsValidBaseKey(apiKey) && HasPermission(apiKey, IngestionApiSecurity.RequiredPermission);

    private static bool IsValidIngestionReadKey(ApiKeyConfiguration apiKey)
        => IsValidBaseKey(apiKey) && HasPermission(apiKey, IngestionApiSecurity.RequiredReadPermission);

    private static bool IsValidBaseKey(ApiKeyConfiguration apiKey)
        => !string.IsNullOrWhiteSpace(apiKey.Key)
            && !string.IsNullOrWhiteSpace(apiKey.ClientId)
            && !string.IsNullOrWhiteSpace(apiKey.ClientName)
            && Guid.TryParse(apiKey.TenantId, out _);

    private static bool HasPermission(ApiKeyConfiguration apiKey, string permission)
        => apiKey.Permissions.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
}
