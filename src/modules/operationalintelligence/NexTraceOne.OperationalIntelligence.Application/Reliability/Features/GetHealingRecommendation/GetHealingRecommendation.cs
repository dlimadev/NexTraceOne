using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetHealingRecommendation;

/// <summary>
/// Feature: GetHealingRecommendation — obtém uma recomendação de self-healing por ID.
/// </summary>
public static class GetHealingRecommendation
{
    public sealed record Query(Guid RecommendationId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.RecommendationId).NotEmpty();
        }
    }

    public sealed class Handler(
        IHealingRecommendationRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var recommendation = await repository.GetByIdAsync(
                HealingRecommendationId.From(request.RecommendationId), cancellationToken);

            if (recommendation is null)
                return ReliabilityErrors.HealingRecommendationNotFound(request.RecommendationId.ToString());

            return Result<Response>.Success(new Response(
                recommendation.Id.Value,
                recommendation.ServiceName,
                recommendation.Environment,
                recommendation.IncidentId,
                recommendation.RootCauseDescription,
                recommendation.ActionType.ToString(),
                recommendation.ActionDetails,
                recommendation.ConfidenceScore,
                recommendation.EstimatedImpact,
                recommendation.RelatedRunbookIds,
                recommendation.HistoricalSuccessRate,
                recommendation.Status.ToString(),
                recommendation.ApprovedByUserId,
                recommendation.ApprovedAt,
                recommendation.ExecutionStartedAt,
                recommendation.ExecutionCompletedAt,
                recommendation.ExecutionResult,
                recommendation.ErrorMessage,
                recommendation.EvidenceTrail,
                recommendation.GeneratedAt));
        }
    }

    public sealed record Response(
        Guid RecommendationId,
        string ServiceName,
        string Environment,
        Guid? IncidentId,
        string RootCauseDescription,
        string ActionType,
        string ActionDetails,
        int ConfidenceScore,
        string? EstimatedImpact,
        string? RelatedRunbookIds,
        decimal? HistoricalSuccessRate,
        string Status,
        string? ApprovedByUserId,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset? ExecutionStartedAt,
        DateTimeOffset? ExecutionCompletedAt,
        string? ExecutionResult,
        string? ErrorMessage,
        string? EvidenceTrail,
        DateTimeOffset GeneratedAt);
}
