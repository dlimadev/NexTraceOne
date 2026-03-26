using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetPostReleaseReview;

/// <summary>
/// Feature: GetPostReleaseReview — retorna a review automática pós-release de uma release,
/// incluindo o estado atual da verificação, fase, outcome, confiança e observações.
///
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetPostReleaseReview
{
    /// <summary>Query para obter a review pós-release de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna a review pós-release e suas janelas de observação.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IPostReleaseReviewRepository reviewRepository,
        IObservationWindowRepository windowRepository,
        IReleaseBaselineRepository baselineRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);

            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var review = await reviewRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            if (review is null)
                return ChangeIntelligenceErrors.PostReleaseReviewNotFound(request.ReleaseId.ToString());

            var windows = await windowRepository.ListByReleaseIdAsync(releaseId, cancellationToken);
            var baseline = await baselineRepository.GetByReleaseIdAsync(releaseId, cancellationToken);

            var windowDtos = windows
                .Select(w => new ObservationWindowDto(
                    w.Id.Value,
                    w.Phase.ToString(),
                    w.StartsAt,
                    w.EndsAt,
                    w.IsCollected,
                    w.CollectedAt,
                    w.RequestsPerMinute,
                    w.ErrorRate,
                    w.AvgLatencyMs,
                    w.P95LatencyMs,
                    w.P99LatencyMs,
                    w.Throughput))
                .ToList();

            BaselineDto? baselineDto = baseline is not null
                ? new BaselineDto(
                    baseline.Id.Value,
                    baseline.RequestsPerMinute,
                    baseline.ErrorRate,
                    baseline.AvgLatencyMs,
                    baseline.P95LatencyMs,
                    baseline.P99LatencyMs,
                    baseline.Throughput,
                    baseline.CollectedFrom,
                    baseline.CollectedTo,
                    baseline.CapturedAt)
                : null;

            return new Response(
                review.Id.Value,
                review.ReleaseId.Value,
                release.ServiceName,
                release.Version,
                release.Environment,
                review.CurrentPhase.ToString(),
                review.Outcome.ToString(),
                review.ConfidenceScore,
                review.Summary,
                review.IsCompleted,
                review.StartedAt,
                review.CompletedAt,
                baselineDto,
                windowDtos);
        }
    }

    /// <summary>DTO de janela de observação.</summary>
    public sealed record ObservationWindowDto(
        Guid Id,
        string Phase,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        bool IsCollected,
        DateTimeOffset? CollectedAt,
        decimal? RequestsPerMinute,
        decimal? ErrorRate,
        decimal? AvgLatencyMs,
        decimal? P95LatencyMs,
        decimal? P99LatencyMs,
        decimal? Throughput);

    /// <summary>DTO de baseline de release.</summary>
    public sealed record BaselineDto(
        Guid Id,
        decimal RequestsPerMinute,
        decimal ErrorRate,
        decimal AvgLatencyMs,
        decimal P95LatencyMs,
        decimal P99LatencyMs,
        decimal Throughput,
        DateTimeOffset CollectedFrom,
        DateTimeOffset CollectedTo,
        DateTimeOffset CapturedAt);

    /// <summary>Resposta da query de review pós-release.</summary>
    public sealed record Response(
        Guid ReviewId,
        Guid ReleaseId,
        string ServiceName,
        string Version,
        string Environment,
        string CurrentPhase,
        string Outcome,
        decimal ConfidenceScore,
        string Summary,
        bool IsCompleted,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt,
        BaselineDto? Baseline,
        IReadOnlyList<ObservationWindowDto> ObservationWindows);
}
