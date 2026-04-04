using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Incidents.Entities;

/// <summary>
/// Entidade que representa um Post-Incident Review (PIR) — processo formal de
/// análise pós-incidente que progride através de fases de investigação.
/// Ligado a um IncidentRecord e opcionalmente a mudanças correlacionadas.
///
/// Ciclo de vida: FactGathering → RootCauseAnalysis → PreventiveActions → FinalReview → Completed.
/// </summary>
public sealed class PostIncidentReview : AuditableEntity<PostIncidentReviewId>
{
    private PostIncidentReview() { }

    /// <summary>Identificador do incidente associado.</summary>
    public Guid IncidentId { get; private set; }

    /// <summary>Fase actual do PIR.</summary>
    public PostIncidentReviewPhase CurrentPhase { get; private set; }

    /// <summary>Resultado do PIR (determinado durante ou após a análise).</summary>
    public PostIncidentReviewOutcome Outcome { get; private set; }

    /// <summary>Resumo da análise de causa raiz.</summary>
    public string? RootCauseAnalysis { get; private set; }

    /// <summary>Ações preventivas identificadas (JSON serializado).</summary>
    public string? PreventiveActionsJson { get; private set; }

    /// <summary>Timeline do incidente documentada durante o PIR.</summary>
    public string? TimelineNarrative { get; private set; }

    /// <summary>Equipa responsável pela condução do PIR.</summary>
    public string ResponsibleTeam { get; private set; } = string.Empty;

    /// <summary>Facilitador do PIR.</summary>
    public string? Facilitator { get; private set; }

    /// <summary>Resumo executivo do PIR.</summary>
    public string? Summary { get; private set; }

    /// <summary>Indica se o PIR foi concluído.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Data/hora de início do PIR.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Data/hora de conclusão do PIR.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Cria um novo PIR na fase inicial de recolha de factos.
    /// </summary>
    public static PostIncidentReview Start(
        PostIncidentReviewId id,
        Guid incidentId,
        string responsibleTeam,
        string? facilitator,
        DateTimeOffset startedAt)
    {
        Guard.Against.Default(incidentId);
        Guard.Against.NullOrWhiteSpace(responsibleTeam);

        return new PostIncidentReview
        {
            Id = id,
            IncidentId = incidentId,
            CurrentPhase = PostIncidentReviewPhase.FactGathering,
            Outcome = PostIncidentReviewOutcome.Pending,
            ResponsibleTeam = responsibleTeam,
            Facilitator = facilitator,
            IsCompleted = false,
            StartedAt = startedAt,
        };
    }

    /// <summary>
    /// Avança o PIR para a próxima fase, atualizando dados de análise.
    /// Transições permitidas: FactGathering→RootCauseAnalysis→PreventiveActions→FinalReview→Completed.
    /// A fase Completed só pode ser atingida a partir de FinalReview.
    /// </summary>
    public void Progress(
        PostIncidentReviewPhase newPhase,
        PostIncidentReviewOutcome? outcome,
        string? rootCauseAnalysis,
        string? preventiveActionsJson,
        string? timelineNarrative,
        string? summary,
        DateTimeOffset? completedAt)
    {
        Guard.Against.EnumOutOfRange(newPhase);

        if (newPhase == PostIncidentReviewPhase.Completed && CurrentPhase != PostIncidentReviewPhase.FinalReview)
        {
            throw new InvalidOperationException(
                $"Cannot complete PIR from phase {CurrentPhase}. Only FinalReview can transition to Completed.");
        }

        if (newPhase != PostIncidentReviewPhase.Completed && (int)newPhase <= (int)CurrentPhase)
        {
            throw new InvalidOperationException(
                $"Cannot move PIR from phase {CurrentPhase} to {newPhase}. Phase must advance forward.");
        }

        CurrentPhase = newPhase;

        if (outcome.HasValue) Outcome = outcome.Value;
        if (rootCauseAnalysis is not null) RootCauseAnalysis = rootCauseAnalysis;
        if (preventiveActionsJson is not null) PreventiveActionsJson = preventiveActionsJson;
        if (timelineNarrative is not null) TimelineNarrative = timelineNarrative;
        if (summary is not null) Summary = summary;

        if (newPhase == PostIncidentReviewPhase.Completed)
        {
            IsCompleted = true;
            CompletedAt = completedAt ?? DateTimeOffset.UtcNow;
        }
    }
}

/// <summary>Identificador fortemente tipado de PostIncidentReview.</summary>
public sealed record PostIncidentReviewId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static PostIncidentReviewId New() => new(Guid.NewGuid());
}
