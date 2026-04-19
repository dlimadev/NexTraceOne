using Microsoft.Extensions.Configuration;

using NexTraceOne.IdentityAccess.Application.Abstractions;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// Implementação de ISamlConfigProvider que lê configuração SAML de IConfiguration.
/// Chaves: Saml:EntityId, Saml:SsoUrl, Saml:SloUrl, Saml:IdpCertificate,
///         Saml:JitProvisioningEnabled, Saml:DefaultRole.
///
/// Retorna null se EntityId ou SsoUrl não estiverem configurados.
/// Em futura iteração, poderá ser substituído por implementação que lê do banco por tenant.
/// </summary>
internal sealed class ConfigurationSamlConfigProvider(IConfiguration configuration) : ISamlConfigProvider
{
    /// <inheritdoc />
    public Task<SamlSsoConfig?> GetActiveConfigAsync(CancellationToken ct)
    {
        var entityId = configuration["Saml:EntityId"];
        var ssoUrl = configuration["Saml:SsoUrl"];

        if (string.IsNullOrWhiteSpace(entityId) || string.IsNullOrWhiteSpace(ssoUrl))
        {
            return Task.FromResult<SamlSsoConfig?>(null);
        }

        var config = new SamlSsoConfig(
            EntityId: entityId,
            SsoUrl: ssoUrl,
            SloUrl: configuration["Saml:SloUrl"] ?? string.Empty,
            IdpCertificate: configuration["Saml:IdpCertificate"] ?? string.Empty,
            JitProvisioningEnabled: configuration.GetValue<bool>("Saml:JitProvisioningEnabled"),
            DefaultRole: configuration["Saml:DefaultRole"] ?? "Viewer");

        return Task.FromResult<SamlSsoConfig?>(config);
    }
}
