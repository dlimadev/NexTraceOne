using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetShowbackReport;

public static class GetShowbackReport
{
    public sealed record Query(
        string? Team,
        string? Domain,
        string? ServiceId,
        string Period) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Period).NotEmpty().MaximumLength(20);
        }
    }

    public sealed class Handler(ICostRecordRepository recordRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var records = request.ServiceId is not null
                ? await recordRepository.ListByServiceAsync(request.ServiceId, request.Period, cancellationToken)
                : request.Team is not null
                    ? await recordRepository.ListByTeamAsync(request.Team, request.Period, cancellationToken)
                    : request.Domain is not null
                        ? await recordRepository.ListByDomainAsync(request.Domain, request.Period, cancellationToken)
                        : await recordRepository.ListByPeriodAsync(request.Period, cancellationToken);

            var totalCost = records.Sum(r => r.TotalCost);

            var byTeam = records
                .GroupBy(r => r.Team ?? "(unassigned)")
                .Select(g => new TeamCostDto(g.Key, g.Sum(r => r.TotalCost), g.Count()))
                .OrderByDescending(t => t.TotalCost)
                .ToList();

            var byDomain = records
                .GroupBy(r => r.Domain ?? "(unassigned)")
                .Select(g => new DomainCostDto(g.Key, g.Sum(r => r.TotalCost), g.Count()))
                .OrderByDescending(d => d.TotalCost)
                .ToList();

            var byService = records
                .GroupBy(r => new { r.ServiceId, r.ServiceName })
                .Select(g => new ServiceCostDto(g.Key.ServiceId, g.Key.ServiceName, g.Sum(r => r.TotalCost)))
                .OrderByDescending(s => s.TotalCost)
                .Take(10)
                .ToList();

            return new Response(totalCost, "USD", request.Period, byTeam, byDomain, byService);
        }
    }

    public sealed record TeamCostDto(string Team, decimal TotalCost, int RecordCount);
    public sealed record DomainCostDto(string Domain, decimal TotalCost, int RecordCount);
    public sealed record ServiceCostDto(string ServiceId, string ServiceName, decimal TotalCost);

    public sealed record Response(
        decimal TotalCost,
        string Currency,
        string Period,
        IReadOnlyList<TeamCostDto> ByTeam,
        IReadOnlyList<DomainCostDto> ByDomain,
        IReadOnlyList<ServiceCostDto> ByService);
}
