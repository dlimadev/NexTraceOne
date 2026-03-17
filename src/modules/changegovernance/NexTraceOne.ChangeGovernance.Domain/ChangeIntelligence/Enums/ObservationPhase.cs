namespace NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

/// <summary>
/// Fases de observação pós-release.
/// A review automática progride por janelas progressivas de observação,
/// cada uma com critérios e thresholds mais rigorosos.
/// </summary>
public enum ObservationPhase
{
    /// <summary>Coleta de baseline antes da release.</summary>
    BaselineCollection = 0,
    /// <summary>Observação inicial imediatamente após deploy (ex: 15-30 min).</summary>
    InitialObservation = 1,
    /// <summary>Review preliminar após período curto (ex: 1-4 horas).</summary>
    PreliminaryReview = 2,
    /// <summary>Review consolidada após período médio (ex: 24-48 horas).</summary>
    ConsolidatedReview = 3,
    /// <summary>Review final com dados suficientes (ex: 7 dias).</summary>
    FinalReview = 4
}
