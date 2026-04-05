using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.DetectCostAnomalies;

public static class DetectCostAnomalies
{
    public sealed record Query(
        string? Environment,
        string Period) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Period).NotEmpty().MaximumLength(20);
            When(x => x.Environment is not null, () =>
                RuleFor(x => x.Environment!).MaximumLength(100));
        }
    }

    public sealed class Handler(
        ICostRecordRepository recordRepository,
        IServiceCostProfileRepository profileRepository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var records = await recordRepository.ListByPeriodAsync(request.Period, cancellationToken);

            var byService = records
                .GroupBy(r => new { r.ServiceId, r.ServiceName })
                .Select(g => new { g.Key.ServiceId, g.Key.ServiceName, TotalCost = g.Sum(r => r.TotalCost) })
                .ToList();

            var anomalies = new List<CostAnomalyDto>();

            foreach (var svc in byService)
            {
                var environment = request.Environment ?? string.Empty;

                var profile = await profileRepository.GetByServiceAndEnvironmentAsync(
                    svc.ServiceId,
                    environment,
                    cancellationToken);

                if (profile?.MonthlyBudget is null)
                    continue;

                var budget = profile.MonthlyBudget.Value;
                var threshold = budget * (profile.AlertThresholdPercent / 100m);

                if (svc.TotalCost > threshold)
                {
                    var deviationPercent = budget > 0
                        ? (svc.TotalCost - budget) / budget * 100m
                        : 0m;

                    anomalies.Add(new CostAnomalyDto(
                        svc.ServiceId,
                        svc.ServiceName,
                        svc.TotalCost,
                        budget,
                        deviationPercent,
                        profile.AlertThresholdPercent));
                }
            }

            return new Response(
                anomalies,
                byService.Count,
                request.Period,
                dateTimeProvider.UtcNow);
        }
    }

    public sealed record CostAnomalyDto(
        string ServiceId,
        string ServiceName,
        decimal ActualCost,
        decimal BudgetLimit,
        decimal DeviationPercent,
        decimal AlertThresholdPercent);

    public sealed record Response(
        IReadOnlyList<CostAnomalyDto> Anomalies,
        int TotalServicesAnalyzed,
        string Period,
        DateTimeOffset AnalyzedAt);
}
