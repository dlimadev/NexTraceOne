namespace NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

/// <summary>
/// Severidade estimada do incidente que o padrão prevê.
/// </summary>
public enum PredictionSeverity
{
    /// <summary>Incidente previsto com impacto baixo.</summary>
    Low,

    /// <summary>Incidente previsto com impacto médio.</summary>
    Medium,

    /// <summary>Incidente previsto com impacto alto.</summary>
    High,

    /// <summary>Incidente previsto com impacto crítico.</summary>
    Critical
}
