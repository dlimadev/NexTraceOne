namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Modo de autenticação utilizado por um provedor de IA para comunicação.
/// </summary>
public enum AuthenticationMode
{
    /// <summary>Sem autenticação (ex: Ollama local).</summary>
    None,

    /// <summary>Autenticação via chave de API (ex: OpenAI, Gemini).</summary>
    ApiKey,

    /// <summary>Autenticação via OAuth 2.0 / OIDC.</summary>
    OAuth2,

    /// <summary>Autenticação via Managed Identity (ex: Azure OpenAI).</summary>
    ManagedIdentity
}
