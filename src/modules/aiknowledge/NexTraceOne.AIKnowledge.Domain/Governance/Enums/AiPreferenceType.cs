namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipo de preferência de IA definida pelo usuário para uma funcionalidade.
/// </summary>
public enum AiPreferenceType
{
    /// <summary>IA desabilitada para esta funcionalidade.</summary>
    Disabled = 0,

    /// <summary>Usar IA interna (provider/modelo configurável).</summary>
    Internal = 1,

    /// <summary>Usar produto de IA externo (ChatGPT, Claude, Gemini, Copilot).</summary>
    ExternalProduct = 2
}
