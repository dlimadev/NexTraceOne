namespace NexTraceOne.AIKnowledge.Domain.Governance.Enums;

/// <summary>
/// Tipo de guarda — indica em que fase do pipeline de IA o guardrail actua.
/// </summary>
public enum GuardrailType
{
    /// <summary>Actua sobre o input do utilizador antes de enviar ao modelo.</summary>
    Input,

    /// <summary>Actua sobre o output do modelo antes de devolver ao utilizador.</summary>
    Output,

    /// <summary>Actua sobre input e output.</summary>
    Both
}
