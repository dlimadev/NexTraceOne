namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Tipos de decisão tomados sobre um workflow de mitigação.
/// Regista a decisão humana ou automatizada no fluxo de aprovação.
/// </summary>
public enum MitigationDecisionType
{
    /// <summary>Aprovado — decisão favorável à execução.</summary>
    Approved = 0,

    /// <summary>Rejeitado — decisão contra a execução.</summary>
    Rejected = 1,

    /// <summary>Escalado — decisão delegada a nível superior.</summary>
    Escalated = 2,

    /// <summary>Diferido — decisão adiada para análise posterior.</summary>
    Deferred = 3
}
