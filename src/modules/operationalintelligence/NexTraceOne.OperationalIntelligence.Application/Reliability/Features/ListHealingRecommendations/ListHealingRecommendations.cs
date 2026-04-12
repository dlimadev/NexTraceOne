using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListHealingRecommendations;

/// <summary>
/// Feature: ListHealingRecommendations — lista recomendações de self-healing com filtros opcionais.
/// Suporta filtro por ServiceName e Status.
/// </summary>
public static class ListHealingRecommendations
{
    public sealed record Query(
        string? ServiceName = null,
        string? Status = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).MaximumLength(200).When(x => x.ServiceName is not null);
            RuleFor(x => x.Status)
                .Must(s => s is null || Enum.TryParse<HealingRecommendationStatus>(s, true, out _))
                .WithMessage("Invalid status. Valid values: Proposed, Approved, Executing, Completed, Failed, Rejected.");
        }
    }

    public sealed class Handler(
        IHealingRecommendationRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            HealingRecommendationStatus? status = null;
            if (request.Status is not null && Enum.TryParse<HealingRecommendationStatus>(request.Status, true, out var parsedStatus))
                status = parsedStatus;

            var recommendations = await repository.ListByStatusAsync(
                status, request.ServiceName, cancellationToken);

            var items = recommendations.Select(r => new RecommendationItem(
                r.Id.Value,
                r.ServiceName,
                r.Environment,
                r.IncidentId,
                r.RootCauseDescription,
                r.ActionType.ToString(),
                r.ConfidenceScore,
                r.HistoricalSuccessRate,
                r.Status.ToString(),
                r.GeneratedAt)).ToList();

            return Result<Response>.Success(new Response(items, items.Count));
        }
    }

    public sealed record RecommendationItem(
        Guid RecommendationId,
        string ServiceName,
        string Environment,
        Guid? IncidentId,
        string RootCauseDescription,
        string ActionType,
        int ConfidenceScore,
        decimal? HistoricalSuccessRate,
        string Status,
        DateTimeOffset GeneratedAt);

    public sealed record Response(IReadOnlyList<RecommendationItem> Items, int TotalCount);
}
