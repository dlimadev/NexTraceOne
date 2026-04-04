namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

/// <summary>
/// Fases do processo de Post-Incident Review (PIR).
/// Cada fase representa um estágio formal da análise pós-incidente.
/// </summary>
public enum PostIncidentReviewPhase
{
    /// <summary>Recolha inicial de factos e timeline do incidente.</summary>
    FactGathering = 0,

    /// <summary>Análise de causa raiz (Root Cause Analysis).</summary>
    RootCauseAnalysis = 1,

    /// <summary>Identificação de ações preventivas e melhorias.</summary>
    PreventiveActions = 2,

    /// <summary>Revisão final e aprovação do relatório PIR.</summary>
    FinalReview = 3,

    /// <summary>PIR concluído e publicado.</summary>
    Completed = 4
}
