namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Modo de operação de uma vinculação feature-modelo no tenant.
/// </summary>
public enum AiBindingMode
{
    /// <summary>Feature opera sem IA (NullProvider / deterministic fallback).</summary>
    Disabled = 0,

    /// <summary>Usa provider interno de IA (como o comportamento atual).</summary>
    Internal = 1,

    /// <summary>Usa produto de IA externo via adapter.</summary>
    ExternalProduct = 2
}
