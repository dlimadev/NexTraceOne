using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilityTrend;

/// <summary>
/// Feature: GetTeamReliabilityTrend — tendência agregada de confiabilidade da equipa.
/// Baseia-se em dados reais de incidentes por equipa para representar o estado atual.
/// Nota: trending histórico profundo requer integração futura com snapshot agregado por equipa.
/// </summary>
public static class GetTeamReliabilityTrend
{
    public sealed record Query(string TeamId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IReliabilityIncidentSurface incidentSurface,
        ICurrentTenant tenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var incidents = await incidentSurface.GetTeamIncidentsAsync(request.TeamId, tenant.Id, cancellationToken);

            var serviceIds = incidents.Select(i => i.ServiceId).Distinct().ToList();
            var totalServices = serviceIds.Count;
            var degradedCount = incidents.Where(i => i.Severity == IncidentSeverity.Major.ToString()).Select(i => i.ServiceId).Distinct().Count();
            var unavailableCount = incidents.Where(i => i.Severity == IncidentSeverity.Critical.ToString()).Select(i => i.ServiceId).Distinct().Count();
            var needsAttentionCount = incidents.Where(i => i.Severity == IncidentSeverity.Minor.ToString() || i.Severity == IncidentSeverity.Warning.ToString()).Select(i => i.ServiceId).Distinct().Count();
            var healthyCount = Math.Max(0, totalServices - unavailableCount - degradedCount - needsAttentionCount);

            var now = DateTimeOffset.UtcNow;
            var currentPoint = new TeamTrendDataPoint(now, totalServices, healthyCount, degradedCount, unavailableCount, needsAttentionCount);

            var direction = unavailableCount > 0 || degradedCount > 0
                ? TrendDirection.Declining
                : TrendDirection.Stable;

            var summary = totalServices == 0
                ? "No active incidents found for this team."
                : $"{healthyCount}/{totalServices} services healthy. {incidents.Count} active incident(s).";

            return Result<Response>.Success(new Response(
                request.TeamId,
                direction,
                "30d",
                summary,
                [currentPoint]));
        }
    }

    public sealed record TeamTrendDataPoint(
        DateTimeOffset Timestamp,
        int TotalServices,
        int HealthyCount,
        int DegradedCount,
        int UnavailableCount,
        int NeedsAttentionCount);

    public sealed record Response(
        string TeamId,
        TrendDirection Direction,
        string Timeframe,
        string Summary,
        IReadOnlyList<TeamTrendDataPoint> DataPoints);
}
