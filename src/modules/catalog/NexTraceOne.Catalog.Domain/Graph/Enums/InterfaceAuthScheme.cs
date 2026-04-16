namespace NexTraceOne.Catalog.Domain.Graph.Enums;

/// <summary>
/// Esquema de autenticação/autorização aplicado à interface de serviço.
/// </summary>
public enum InterfaceAuthScheme
{
    /// <summary>Sem autenticação.</summary>
    None = 0,

    /// <summary>Autenticação por API Key.</summary>
    ApiKey = 1,

    /// <summary>OAuth 2.0.</summary>
    OAuth2 = 2,

    /// <summary>mTLS — Mutual TLS.</summary>
    MutualTls = 3,

    /// <summary>SAML 2.0.</summary>
    Saml = 4,

    /// <summary>OpenID Connect.</summary>
    OpenIdConnect = 5,

    /// <summary>HTTP Basic Authentication.</summary>
    Basic = 6
}
