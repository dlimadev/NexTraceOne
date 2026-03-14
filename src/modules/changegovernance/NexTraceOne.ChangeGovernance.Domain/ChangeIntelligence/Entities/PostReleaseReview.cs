using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.ChangeIntelligence.Domain.Enums;

namespace NexTraceOne.ChangeIntelligence.Domain.Entities;

/// <summary>
/// Review automática pós-release baseada em janelas progressivas de observação.
/// A review nunca é instantânea — ela progride por fases:
/// baseline → observação inicial → review preliminar → consolidada → final.
/// Cada progressão compara indicadores observados com o baseline, considerando
/// volume mínimo de tráfego, tempo suficiente de observação, e confiança dos dados.
/// </summary>
public sealed class PostReleaseReview : AggregateRoot<PostReleaseReviewId>
{
    /// <summary>Release avaliada por esta review.</summary>
    public ReleaseId ReleaseId { get; private set; } = null!;

    /// <summary>Fase atual da observação.</summary>
    public ObservationPhase CurrentPhase { get; private set; }

    /// <summary>Classificação do resultado da release.</summary>
    public ReviewOutcome Outcome { get; private set; }

    /// <summary>Score de confiança da classificação (0.0 a 1.0).</summary>
    public decimal ConfidenceScore { get; private set; }

    /// <summary>Resumo textual da review para exibição em UI e auditoria.</summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>Indica se a review foi concluída (fase final ou dados suficientes).</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Momento de criação da review.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Momento de conclusão da review.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    private PostReleaseReview() { }

    /// <summary>
    /// Inicia uma nova review pós-release na fase de observação inicial.
    /// </summary>
    public static PostReleaseReview Start(
        ReleaseId releaseId,
        DateTimeOffset startedAt)
    {
        Guard.Against.Null(releaseId, nameof(releaseId));

        return new PostReleaseReview
        {
            Id = PostReleaseReviewId.New(),
            ReleaseId = releaseId,
            CurrentPhase = ObservationPhase.InitialObservation,
            Outcome = ReviewOutcome.Inconclusive,
            ConfidenceScore = 0m,
            Summary = string.Empty,
            IsCompleted = false,
            StartedAt = startedAt
        };
    }

    /// <summary>
    /// Progride a review para a próxima fase com base nos indicadores observados.
    /// A classificação é atualizada a cada progressão conforme os dados disponíveis.
    /// </summary>
    public Result<Unit> Progress(
        ObservationPhase newPhase,
        ReviewOutcome outcome,
        decimal confidenceScore,
        string summary,
        DateTimeOffset? completedAt = null)
    {
        if (IsCompleted)
            return Error.Conflict(
                "change_intelligence.review.already_completed",
                "Post-release review has already been completed.");

        if (newPhase <= CurrentPhase)
            return Error.Validation(
                "change_intelligence.review.invalid_phase_progression",
                "Review phase must advance forward, not backward.");

        if (confidenceScore < 0m || confidenceScore > 1m)
            return Error.Validation(
                "change_intelligence.review.invalid_confidence",
                "Confidence score must be between 0.0 and 1.0.");

        Guard.Against.NullOrWhiteSpace(summary, nameof(summary));

        CurrentPhase = newPhase;
        Outcome = outcome;
        ConfidenceScore = confidenceScore;
        Summary = summary;

        if (newPhase == ObservationPhase.FinalReview || confidenceScore >= 0.9m)
        {
            IsCompleted = true;
            CompletedAt = completedAt;
        }

        return Result<Unit>.Success(Unit.Value);
    }
}

/// <summary>Identificador fortemente tipado para PostReleaseReview.</summary>
public sealed record PostReleaseReviewId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Gera um novo identificador.</summary>
    public static PostReleaseReviewId New() => new(Guid.NewGuid());
    /// <summary>Cria a partir de um Guid existente.</summary>
    public static PostReleaseReviewId From(Guid id) => new(id);
}
