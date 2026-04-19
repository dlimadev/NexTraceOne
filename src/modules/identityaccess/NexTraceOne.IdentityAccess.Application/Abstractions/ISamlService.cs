namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Serviço de protocolo SAML 2.0 — constrói AuthnRequests e valida SAMLResponses.
/// Utiliza apenas APIs do BCL .NET: System.Xml, System.Security.Cryptography.Xml e X509Certificates.
/// </summary>
public interface ISamlService
{
    /// <summary>
    /// Gera a URL de redirect para o IdP com o AuthnRequest deflacionado, base64-codificado e URL-encoded.
    /// </summary>
    /// <param name="ssoUrl">URL de SSO do IdP.</param>
    /// <param name="spEntityId">EntityId do Service Provider.</param>
    /// <param name="acsUrl">URL do Assertion Consumer Service do SP.</param>
    /// <param name="requestId">Identificador único da request (prefixado com '_').</param>
    /// <param name="relayState">Estado opaco a preservar no roundtrip (ex: returnTo URL).</param>
    /// <returns>URL completa de redirect para o IdP.</returns>
    string BuildAuthnRequestUrl(
        string ssoUrl,
        string spEntityId,
        string acsUrl,
        string requestId,
        string relayState);

    /// <summary>
    /// Descodifica e valida uma SAMLResponse recebida do IdP.
    /// Verifica a assinatura X.509 e extrai os dados da assertion.
    /// </summary>
    /// <param name="samlResponseBase64">SAMLResponse em base64 (form field SAMLResponse).</param>
    /// <param name="idpCertificatePem">Certificado PEM do IdP para validação de assinatura.</param>
    /// <returns>Dados extraídos da assertion SAML.</returns>
    /// <exception cref="InvalidOperationException">Se a assinatura for inválida ou a assertion estiver malformada.</exception>
    SamlParsedAssertion ParseSamlResponse(string samlResponseBase64, string idpCertificatePem);
}

/// <summary>
/// Dados extraídos de uma SAMLAssertion validada.
/// </summary>
/// <param name="NameId">Identificador do utilizador no IdP (saml:NameID).</param>
/// <param name="Email">Endereço de email do utilizador (atributo email ou EmailAddress).</param>
/// <param name="Name">Nome de exibição do utilizador (atributo cn ou displayName), opcional.</param>
/// <param name="Groups">Grupos/roles do utilizador (atributos groups ou memberOf).</param>
public sealed record SamlParsedAssertion(
    string NameId,
    string Email,
    string? Name,
    IReadOnlyList<string> Groups);
