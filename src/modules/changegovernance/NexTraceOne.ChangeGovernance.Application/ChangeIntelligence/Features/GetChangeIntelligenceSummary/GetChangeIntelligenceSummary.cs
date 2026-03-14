using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Enums;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.ChangeIntelligence.Application.Abstractions;
using NexTraceOne.ChangeIntelligence.Domain.Entities;
using NexTraceOne.ChangeIntelligence.Domain.Enums;
using NexTraceOne.ChangeIntelligence.Domain.Errors;

namespace NexTraceOne.ChangeIntelligence.Application.Features.GetChangeIntelligenceSummary;

/// <summary>
/// Feature: GetChangeIntelligenceSummary — agrega todos os dados de inteligência de uma release num único sumário.
/// É a visão central do Change Intelligence Record.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetChangeIntelligenceSummary
{
    /// <summary>Query para obter o resumo completo de inteligência de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de sumário de inteligência.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que agrega todos os dados de inteligência de uma release num único sumário.
    /// Consulta score, blast radius, markers, baseline, review e rollback assessment.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IChangeScoreRepository scoreRepository,
        IBlastRadiusRepository blastRadiusRepository,
        IExternalMarkerRepository markerRepository,
        IReleaseBaselineRepository baselineRepository,
        IPostReleaseReviewRepository reviewRepository,
        IRollbackAssessmentRepository rollbackRepository,
        IChangeEventRepository eventRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var score = await scoreRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            var blastRadius = await blastRadiusRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            var markers = await markerRepository.ListByReleaseIdAsync(releaseId, cancellationToken);
            var baseline = await baselineRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            var review = await reviewRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            var rollback = await rollbackRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            var events = await eventRepository.ListByReleaseIdAsync(releaseId, cancellationToken);

            var releaseDto = new ReleaseDto(
                release.Id.Value, release.ApiAssetId, release.ServiceName, release.Version,
                release.Environment, release.Status.ToString(), release.ChangeLevel,
                release.ChangeScore, release.WorkItemReference, release.CreatedAt);

            var scoreDto = score is not null
                ? new ScoreDto(score.Score, score.BreakingChangeWeight, score.BlastRadiusWeight,
                    score.EnvironmentWeight, score.ComputedAt)
                : null;

            var blastDto = blastRadius is not null
                ? new BlastRadiusDto(blastRadius.TotalAffectedConsumers, blastRadius.DirectConsumers,
                    blastRadius.TransitiveConsumers, blastRadius.CalculatedAt)
                : null;

            var markerDtos = markers.Select(m => new MarkerDto(
                m.Id.Value, m.MarkerType.ToString(), m.SourceSystem,
                m.ExternalId, m.OccurredAt)).ToList();

            var baselineDto = baseline is not null
                ? new BaselineDto(baseline.RequestsPerMinute, baseline.ErrorRate, baseline.AvgLatencyMs,
                    baseline.P95LatencyMs, baseline.P99LatencyMs, baseline.Throughput,
                    baseline.CollectedFrom, baseline.CollectedTo)
                : null;

            var reviewDto = review is not null
                ? new ReviewDto(review.CurrentPhase.ToString(), review.Outcome.ToString(),
                    review.ConfidenceScore, review.Summary, review.IsCompleted,
                    review.StartedAt, review.CompletedAt)
                : null;

            var rollbackDto = rollback is not null
                ? new RollbackDto(rollback.IsViable, rollback.ReadinessScore, rollback.PreviousVersion,
                    rollback.HasReversibleMigrations, rollback.ConsumersAlreadyMigrated,
                    rollback.TotalConsumersImpacted, rollback.Recommendation, rollback.AssessedAt)
                : null;

            var timelineDtos = events.Select(e => new TimelineEventDto(
                e.Id.Value, e.EventType, e.Description, e.Source, e.OccurredAt)).ToList();

            return new Response(
                releaseDto, scoreDto, blastDto, markerDtos,
                baselineDto, reviewDto, rollbackDto, timelineDtos);
        }
    }

    /// <summary>DTO de release.</summary>
    public sealed record ReleaseDto(
        Guid Id, Guid ApiAssetId, string ServiceName, string Version,
        string Environment, string Status, ChangeLevel ChangeLevel,
        decimal ChangeScore, string? WorkItemReference, DateTimeOffset CreatedAt);

    /// <summary>DTO de score de risco.</summary>
    public sealed record ScoreDto(
        decimal Score, decimal BreakingChangeWeight,
        decimal BlastRadiusWeight, decimal EnvironmentWeight, DateTimeOffset ComputedAt);

    /// <summary>DTO de blast radius.</summary>
    public sealed record BlastRadiusDto(
        int TotalAffectedConsumers, IReadOnlyList<string> DirectConsumers,
        IReadOnlyList<string> TransitiveConsumers, DateTimeOffset CalculatedAt);

    /// <summary>DTO de marcador externo.</summary>
    public sealed record MarkerDto(
        Guid Id, string MarkerType, string SourceSystem,
        string ExternalId, DateTimeOffset OccurredAt);

    /// <summary>DTO de baseline.</summary>
    public sealed record BaselineDto(
        decimal RequestsPerMinute, decimal ErrorRate, decimal AvgLatencyMs,
        decimal P95LatencyMs, decimal P99LatencyMs, decimal Throughput,
        DateTimeOffset CollectedFrom, DateTimeOffset CollectedTo);

    /// <summary>DTO de review pós-release.</summary>
    public sealed record ReviewDto(
        string CurrentPhase, string Outcome, decimal ConfidenceScore,
        string Summary, bool IsCompleted, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt);

    /// <summary>DTO de avaliação de rollback.</summary>
    public sealed record RollbackDto(
        bool IsViable, decimal ReadinessScore, string? PreviousVersion,
        bool HasReversibleMigrations, int ConsumersAlreadyMigrated,
        int TotalConsumersImpacted, string Recommendation, DateTimeOffset AssessedAt);

    /// <summary>DTO de evento da timeline.</summary>
    public sealed record TimelineEventDto(
        Guid Id, string EventType, string Description,
        string Source, DateTimeOffset OccurredAt);

    /// <summary>
    /// Resposta agregada com toda a inteligência da mudança.
    /// Reúne release, scores, blast radius, markers, baseline, review e rollback assessment.
    /// </summary>
    public sealed record Response(
        ReleaseDto Release,
        ScoreDto? Score,
        BlastRadiusDto? BlastRadius,
        IReadOnlyList<MarkerDto> Markers,
        BaselineDto? Baseline,
        ReviewDto? PostReleaseReview,
        RollbackDto? RollbackAssessment,
        IReadOnlyList<TimelineEventDto> Timeline);
}
