using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetCarbonScoreReport;

/// <summary>
/// Feature: GetCarbonScoreReport — relatório de emissões de carbono por período e tenant.
/// W6-04: GreenOps / Carbon Score.
/// </summary>
public static class GetCarbonScoreReport
{
    public sealed record Query(
        DateOnly? From = null,
        DateOnly? To = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.From).LessThanOrEqualTo(x => x.To)
                .When(x => x.From.HasValue && x.To.HasValue);
        }
    }

    public sealed record ServiceCarbonEntry(
        Guid ServiceId,
        double TotalCarbonGrams,
        double TotalCpuHours,
        double TotalMemoryGbHours,
        double TotalNetworkGb);

    public sealed record Response(
        double TotalCarbonGrams,
        IReadOnlyList<ServiceCarbonEntry> TopServices,
        DateOnly From,
        DateOnly To);

    internal sealed class Handler(
        ICarbonScoreRepository repository,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var today = clock.UtcToday;
            var from = request.From ?? today.AddDays(-30);
            var to = request.To ?? today;

            var records = await repository.ListByTenantAndPeriodAsync(
                currentTenant.Id, from, to, cancellationToken);

            var grouped = records
                .GroupBy(r => r.ServiceId)
                .Select(g => new ServiceCarbonEntry(
                    g.Key,
                    g.Sum(r => r.CarbonGrams),
                    g.Sum(r => r.CpuHours),
                    g.Sum(r => r.MemoryGbHours),
                    g.Sum(r => r.NetworkGb)))
                .OrderByDescending(e => e.TotalCarbonGrams)
                .Take(5)
                .ToList();

            return new Response(records.Sum(r => r.CarbonGrams), grouped, from, to);
        }
    }
}
