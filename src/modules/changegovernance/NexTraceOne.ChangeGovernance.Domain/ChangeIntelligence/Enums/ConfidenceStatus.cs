namespace NexTraceOne.ChangeIntelligence.Domain.Enums;

/// <summary>
/// Status de confiança de uma mudança no NexTraceOne.
/// Indica o nível de confiança operacional após a análise de evidências.
/// </summary>
public enum ConfidenceStatus
{
    /// <summary>Confiança ainda não avaliada.</summary>
    NotAssessed = 0,

    /// <summary>Mudança validada com confiança alta.</summary>
    Validated = 1,

    /// <summary>Mudança requer atenção — indicadores parciais.</summary>
    NeedsAttention = 2,

    /// <summary>Suspeita de regressão baseada em evidências.</summary>
    SuspectedRegression = 3,

    /// <summary>Mudança correlacionada com incidente.</summary>
    CorrelatedWithIncident = 4,

    /// <summary>Mudança mitigada após análise.</summary>
    Mitigated = 5
}
