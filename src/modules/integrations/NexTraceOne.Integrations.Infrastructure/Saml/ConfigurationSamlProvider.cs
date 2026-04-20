using Microsoft.Extensions.Configuration;
using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Integrations.Infrastructure.Saml;

/// <summary>
/// Implementação real de <see cref="ISamlProvider"/> baseada em <c>IConfiguration</c>.
/// Considerada <em>configurada</em> quando tanto <c>Saml:EntityId</c> como <c>Saml:SsoUrl</c>
/// estiverem presentes e não-vazios — as mesmas condições verificadas pelo
/// <c>ConfigurationSamlConfigProvider</c> no módulo IdentityAccess.
/// </summary>
internal sealed class ConfigurationSamlProvider(IConfiguration configuration) : ISamlProvider
{
    /// <inheritdoc />
    public bool IsConfigured
        => !string.IsNullOrWhiteSpace(configuration["Saml:EntityId"])
        && !string.IsNullOrWhiteSpace(configuration["Saml:SsoUrl"]);
}
