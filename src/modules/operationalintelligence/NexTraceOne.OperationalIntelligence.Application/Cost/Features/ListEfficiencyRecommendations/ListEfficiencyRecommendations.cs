using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.ListEfficiencyRecommendations;

public static class ListEfficiencyRecommendations
{
    public sealed record Query(
        string? ServiceId,
        string? Environment,
        bool UnacknowledgedOnly = false) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            When(x => x.ServiceId is not null, () =>
                RuleFor(x => x.ServiceId!).MaximumLength(200));
            When(x => x.Environment is not null, () =>
                RuleFor(x => x.Environment!).MaximumLength(100));
        }
    }

    public sealed class Handler(IEfficiencyRecommendationRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var items = request.UnacknowledgedOnly
                ? await repository.ListUnacknowledgedAsync(cancellationToken)
                : request.ServiceId is not null && request.Environment is not null
                    ? await repository.ListByServiceAsync(request.ServiceId, request.Environment, cancellationToken)
                    : await repository.ListUnacknowledgedAsync(cancellationToken);

            var dtos = items
                .Select(r => new RecommendationDto(
                    r.Id.Value,
                    r.ServiceId,
                    r.ServiceName,
                    r.ServiceCost,
                    r.MedianPeerCost,
                    r.DeviationPercent,
                    r.RecommendationText,
                    r.Priority))
                .ToList();

            return new Response(dtos);
        }
    }

    public sealed record RecommendationDto(
        Guid Id,
        string ServiceId,
        string ServiceName,
        decimal ServiceCost,
        decimal MedianPeerCost,
        decimal DeviationPercent,
        string RecommendationText,
        string Priority);

    public sealed record Response(IReadOnlyList<RecommendationDto> Items);
}
