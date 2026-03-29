using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetUnifiedTimeline;

/// <summary>
/// Query para timeline unificada de eventos legacy e modernos.
/// Combina incidentes, eventos de batch, MQ e mainframe.
/// Fase 1: utiliza dados de incidentes já persistidos em PostgreSQL.
/// Fase 2 (futura): integrará eventos analíticos do ClickHouse.
/// </summary>
public static class GetUnifiedTimeline
{
    public sealed record Query(
        string? ServiceName,
        string? SystemName,
        string? Environment,
        DateTimeOffset? From,
        DateTimeOffset? To,
        int PageSize = 50,
        int Page = 1) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.ServiceName).MaximumLength(200);
            RuleFor(x => x.SystemName).MaximumLength(100);
            RuleFor(x => x.Environment).MaximumLength(100);
        }
    }

    public sealed class Handler(
        IIncidentStore incidentStore,
        ILogger<Handler> logger) : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Building unified timeline for service={Service}, system={System}",
                request.ServiceName, request.SystemName);

            var entries = new List<TimelineEntry>();

            // Phase 1: Include incidents from PostgreSQL via IIncidentStore
            try
            {
                var incidents = incidentStore.GetIncidentListItems();

                foreach (var incident in incidents)
                {
                    var matchesService = string.IsNullOrWhiteSpace(request.ServiceName) ||
                        string.Equals(incident.ServiceDisplayName, request.ServiceName, StringComparison.OrdinalIgnoreCase);
                    var matchesEnvironment = string.IsNullOrWhiteSpace(request.Environment) ||
                        string.Equals(incident.Environment, request.Environment, StringComparison.OrdinalIgnoreCase);
                    var matchesFrom = !request.From.HasValue || incident.CreatedAt >= request.From.Value;
                    var matchesTo = !request.To.HasValue || incident.CreatedAt <= request.To.Value;

                    if (!matchesService || !matchesEnvironment || !matchesFrom || !matchesTo)
                        continue;

                    entries.Add(new TimelineEntry(
                        Id: incident.IncidentId.ToString(),
                        Source: "incident",
                        EventType: incident.IncidentType.ToString(),
                        Title: incident.Title,
                        Severity: incident.Severity.ToString(),
                        ServiceName: incident.ServiceDisplayName,
                        SystemName: null,
                        Timestamp: incident.CreatedAt,
                        Details: new Dictionary<string, string>
                        {
                            ["status"] = incident.Status.ToString(),
                            ["reference"] = incident.Reference ?? "",
                            ["has_correlation"] = incident.HasCorrelatedChanges.ToString()
                        }));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch incidents for unified timeline");
            }

            // Phase 2: Legacy events from ClickHouse would be added here
            // This is a placeholder for future ClickHouse integration

            var totalCount = entries.Count;
            var ordered = entries
                .OrderByDescending(e => e.Timestamp)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return Task.FromResult(Result<Response>.Success(new Response(ordered, totalCount)));
        }
    }

    public sealed record TimelineEntry(
        string Id,
        string Source,
        string EventType,
        string? Title,
        string? Severity,
        string? ServiceName,
        string? SystemName,
        DateTimeOffset Timestamp,
        Dictionary<string, string>? Details);

    public sealed record Response(
        List<TimelineEntry> Entries,
        int TotalCount);
}
