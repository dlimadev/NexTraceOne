using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.CorrelateCloudCostWithChange;

public static class CorrelateCloudCostWithChange
{
    public sealed record Query(
        Guid ChangeId,
        string ServiceId,
        string Environment,
        string Period) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ChangeId).NotEmpty();
            RuleFor(x => x.ServiceId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Period).NotEmpty().MaximumLength(20);
        }
    }

    public sealed class Handler(ICostRecordRepository recordRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var serviceRecords = await recordRepository.ListByServiceAsync(
                request.ServiceId,
                request.Period,
                cancellationToken);

            var releaseRecords = await recordRepository.ListByReleaseAsync(
                request.ChangeId,
                cancellationToken);

            var totalCostForService = serviceRecords.Sum(r => r.TotalCost);
            var costAttributedToChange = releaseRecords.Sum(r => r.TotalCost);
            var costAttributionPercent = totalCostForService > 0
                ? costAttributedToChange / totalCostForService * 100m
                : 0m;

            return new Response(
                request.ChangeId,
                request.ServiceId,
                request.Environment,
                request.Period,
                totalCostForService,
                costAttributedToChange,
                costAttributionPercent,
                releaseRecords.Count,
                "USD");
        }
    }

    public sealed record Response(
        Guid ChangeId,
        string ServiceId,
        string Environment,
        string Period,
        decimal TotalCostForService,
        decimal CostAttributedToChange,
        decimal CostAttributionPercent,
        int CorrelatedRecordCount,
        string Currency);
}
