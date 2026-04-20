namespace NexTraceOne.Integrations.Domain;

/// <summary>
/// Contrato de diagnóstico do provider SAML 2.0 para o dashboard de system health.
///
/// Expõe apenas o sinal <c>IsConfigured</c>, seguindo o padrão dos restantes providers
/// opcionais (<see cref="ICanaryProvider"/>, <see cref="IBackupProvider"/>, etc.).
/// A implementação real lê as chaves <c>Saml:EntityId</c> e <c>Saml:SsoUrl</c> de
/// <c>IConfiguration</c>; a implementação <c>NullSamlProvider</c> é registada por defeito
/// quando essas chaves estão ausentes.
///
/// Para a lógica completa de SAML (construir AuthnRequests, validar SAMLResponses, JIT
/// provisioning, …) ver <c>ISamlService</c> e <c>ISamlConfigProvider</c> em
/// <c>NexTraceOne.IdentityAccess.Application</c>.
/// </summary>
public interface ISamlProvider
{
    /// <summary>
    /// Indica se o IdP SAML 2.0 está configurado neste ambiente.
    /// Quando false, o fluxo <c>StartSamlLogin</c> retorna <c>SamlNotConfigured</c>
    /// e o dashboard de system health mostra o provider como "Not configured".
    /// </summary>
    bool IsConfigured { get; }
}
