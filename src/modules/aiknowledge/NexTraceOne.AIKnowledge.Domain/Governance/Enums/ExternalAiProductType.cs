namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Produto de IA externo suportado pela plataforma.
/// Cada produto é mapeado internamente para um provider de API correspondente.
/// </summary>
public enum ExternalAiProductType
{
    /// <summary>OpenAI ChatGPT.</summary>
    ChatGPT = 0,

    /// <summary>Anthropic Claude / Claude Code.</summary>
    ClaudeCode = 1,

    /// <summary>Google Gemini.</summary>
    Gemini = 2,

    /// <summary>GitHub Copilot.</summary>
    GitHubCopilot = 3,

    /// <summary>Outro produto configurável.</summary>
    Custom = 99
}
