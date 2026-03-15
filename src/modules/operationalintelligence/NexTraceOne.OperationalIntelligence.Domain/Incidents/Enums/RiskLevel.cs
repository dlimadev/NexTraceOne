namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Nível de risco associado a uma ação ou workflow de mitigação.
/// Permite avaliar e priorizar ações com base no impacto potencial.
/// </summary>
public enum RiskLevel
{
    /// <summary>Risco baixo — impacto mínimo esperado.</summary>
    Low = 0,

    /// <summary>Risco médio — impacto moderado possível.</summary>
    Medium = 1,

    /// <summary>Risco alto — impacto significativo provável.</summary>
    High = 2,

    /// <summary>Risco crítico — impacto severo em produção.</summary>
    Critical = 3
}
