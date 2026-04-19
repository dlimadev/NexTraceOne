namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Fornece a configuração SAML 2.0 activa para o tenant corrente.
/// A implementação padrão lê de IConfiguration (Saml:*).
/// Em futura iteração, poderá resolver a config por tenant via base de dados.
/// </summary>
public interface ISamlConfigProvider
{
    /// <summary>
    /// Obtém a configuração SAML activa.
    /// Retorna null se EntityId ou SsoUrl não estiverem configurados.
    /// </summary>
    Task<SamlSsoConfig?> GetActiveConfigAsync(CancellationToken ct);
}

/// <summary>
/// Configuração SAML 2.0 para fluxo de SSO.
/// </summary>
/// <param name="EntityId">EntityID do Service Provider.</param>
/// <param name="SsoUrl">URL de Single Sign-On do IdP.</param>
/// <param name="SloUrl">URL de Single Logout do IdP.</param>
/// <param name="IdpCertificate">Certificado PEM do IdP para validação de assinaturas.</param>
/// <param name="JitProvisioningEnabled">Indica se o Just-in-Time provisioning está activo.</param>
/// <param name="DefaultRole">Role padrão para novos utilizadores provisionados via SAML JIT.</param>
public sealed record SamlSsoConfig(
    string EntityId,
    string SsoUrl,
    string SloUrl,
    string IdpCertificate,
    bool JitProvisioningEnabled,
    string DefaultRole);
