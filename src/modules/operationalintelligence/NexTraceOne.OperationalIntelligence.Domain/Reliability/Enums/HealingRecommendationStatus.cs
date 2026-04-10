namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Estado do ciclo de vida de uma recomendação de self-healing.
/// Ciclo: Proposed → Approved → Executing → Completed | Failed.
/// Alternativo: Proposed → Rejected.
/// </summary>
public enum HealingRecommendationStatus
{
    /// <summary>Recomendação gerada, aguarda aprovação.</summary>
    Proposed = 0,

    /// <summary>Aprovada, pronta para execução.</summary>
    Approved = 1,

    /// <summary>Em execução.</summary>
    Executing = 2,

    /// <summary>Execução concluída com sucesso.</summary>
    Completed = 3,

    /// <summary>Execução falhou.</summary>
    Failed = 4,

    /// <summary>Rejeitada pelo operador — não será executada.</summary>
    Rejected = 5,
}
