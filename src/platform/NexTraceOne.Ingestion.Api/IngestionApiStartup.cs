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
    /// configurada. Em Development: regista um aviso e continua se não houver chaves válidas.
    /// Em produção: lança <see cref="InvalidOperationException"/> impedindo o arranque.
    /// </summary>
    internal static void ValidateIngestionSecurityConfiguration(this WebApplication app)
    {
        var configuredKeys = app.Configuration
            .GetSection("Security:ApiKeys")
            .Get<List<ApiKeyConfiguration>>() ?? [];

        var validKeys = configuredKeys
            .Where(IsValidIngestionApiKey)
            .ToList();

        if (validKeys.Count == 0)
        {
            const string message =
                "Ingestion.Api requires at least one API key with a valid tenant and " +
                "'integrations:write' permission configured under 'Security:ApiKeys'.";

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
            "Ingestion.Api security initialized with {ApiKeyCount} API key client(s): {ClientIds}",
            validKeys.Count,
            string.Join(", ", validKeys.Select(key => key.ClientId)));
    }

    private static bool IsValidIngestionApiKey(ApiKeyConfiguration apiKey)
        => !string.IsNullOrWhiteSpace(apiKey.Key)
            && !string.IsNullOrWhiteSpace(apiKey.ClientId)
            && !string.IsNullOrWhiteSpace(apiKey.ClientName)
            && Guid.TryParse(apiKey.TenantId, out _)
            && apiKey.Permissions.Any(permission =>
                string.Equals(permission, IngestionApiSecurity.RequiredPermission, StringComparison.OrdinalIgnoreCase));
}
