namespace NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

/// <summary>
/// Nível de risco global de um serviço calculado pelo Risk Center.
/// Usado para priorização de atenção operacional e governança.
/// </summary>
public enum RiskLevel
{
    /// <summary>Risco negligenciável — serviço estável e bem governado.</summary>
    Negligible = 0,

    /// <summary>Risco baixo — atenção normal.</summary>
    Low = 1,

    /// <summary>Risco médio — requer revisão.</summary>
    Medium = 2,

    /// <summary>Risco alto — ação necessária.</summary>
    High = 3,

    /// <summary>Risco crítico — intervenção imediata.</summary>
    Critical = 4
}
