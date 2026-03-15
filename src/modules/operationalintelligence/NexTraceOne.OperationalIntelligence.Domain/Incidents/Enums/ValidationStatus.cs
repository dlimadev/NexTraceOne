namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Estado da validação pós-mitigação.
/// Permite rastrear se a ação corretiva foi devidamente verificada.
/// </summary>
public enum ValidationStatus
{
    /// <summary>Validação pendente — ainda não iniciada.</summary>
    Pending = 0,

    /// <summary>Validação em andamento.</summary>
    InProgress = 1,

    /// <summary>Validação aprovada — mitigação confirmada como eficaz.</summary>
    Passed = 2,

    /// <summary>Validação falhada — mitigação não atingiu o resultado esperado.</summary>
    Failed = 3,

    /// <summary>Validação ignorada — não aplicável neste contexto.</summary>
    Skipped = 4
}
