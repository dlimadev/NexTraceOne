namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Estado atual de um SLO face ao objetivo definido e ao error budget disponível.
/// </summary>
public enum SloStatus
{
    /// <summary>SLO a ser cumprido — error budget dentro dos limites esperados.</summary>
    Healthy = 0,

    /// <summary>SLO em risco — error budget consumido acima do patamar de alerta.</summary>
    AtRisk = 1,

    /// <summary>SLO violado — error budget esgotado no período de medição.</summary>
    Violated = 2
}
