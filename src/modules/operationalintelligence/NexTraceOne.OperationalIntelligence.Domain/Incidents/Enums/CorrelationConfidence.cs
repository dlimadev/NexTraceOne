namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Nível de confiança na correlação entre incidente e causa provável.
/// Preparado para futura automação via IA operacional.
/// </summary>
public enum CorrelationConfidence
{
    /// <summary>Correlação não avaliada.</summary>
    NotAssessed = 0,

    /// <summary>Correlação fraca — baseada apenas em proximidade temporal.</summary>
    Low = 1,

    /// <summary>Correlação moderada — evidência parcial disponível.</summary>
    Medium = 2,

    /// <summary>Correlação forte — evidência consistente identificada.</summary>
    High = 3,

    /// <summary>Correlação confirmada — validada manualmente ou por automação.</summary>
    Confirmed = 4
}
