using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Integrations.Infrastructure.Saml;

/// <summary>
/// Implementação nula de <see cref="ISamlProvider"/>.
/// Registada por defeito quando as chaves de configuração SAML (<c>Saml:EntityId</c> e
/// <c>Saml:SsoUrl</c>) não estão presentes em <c>IConfiguration</c>.
/// Enquanto activa, <c>StartSamlLogin</c> no módulo IdentityAccess retorna
/// <c>SamlNotConfigured</c> e o dashboard de system health marca SAML como "Not configured".
/// </summary>
internal sealed class NullSamlProvider : ISamlProvider
{
    /// <inheritdoc />
    public bool IsConfigured => false;
}
