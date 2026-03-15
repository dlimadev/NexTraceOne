namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Resultado final de uma mitigação executada.
/// Permite classificar o desfecho da ação corretiva aplicada.
/// </summary>
public enum MitigationOutcome
{
    /// <summary>Mitigação bem-sucedida — problema resolvido.</summary>
    Successful = 0,

    /// <summary>Mitigação parcialmente bem-sucedida — problema atenuado.</summary>
    PartiallySuccessful = 1,

    /// <summary>Mitigação falhada — problema persiste.</summary>
    Failed = 2,

    /// <summary>Resultado inconclusivo — necessita análise adicional.</summary>
    Inconclusive = 3,

    /// <summary>Mitigação cancelada antes da conclusão.</summary>
    Cancelled = 4
}
