using FluentValidation;
using Microsoft.Extensions.Logging;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Contracts.ChangeIntelligence.ServiceInterfaces;
using NexTraceOne.ChangeGovernance.Contracts.RulesetGovernance.ServiceInterfaces;
using NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.GetDashboardAnnotations;

/// <summary>
/// Feature: GetDashboardAnnotations — agrega eventos de anotação para sobreposição
/// em widgets de séries temporais de um dashboard.
///
/// Fontes de anotação:
/// — Changes (releases, rollouts) via IChangeIntelligenceModule
/// — Incidents (abertura, resolução) via IIncidentModule
/// — Policy Violations via IRulesetGovernanceModule
/// — Contract Breaking Changes (not yet bridged — honest gap)
///
/// Cada fonte é isolada com try/catch: uma falha de módulo retorna IsSimulated: true
/// para essa fonte, sem abortar as restantes.
///
/// Wave V3.2 — Query-driven Widgets &amp; Widget SDK.
/// </summary>
public static class GetDashboardAnnotations
{
    public sealed record Query(
        string TenantId,
        DateTimeOffset From,
        DateTimeOffset To,
        IReadOnlyList<string>? ServiceNames = null,
        int MaxPerSource = 50) : IQuery<Response>;

    public sealed record AnnotationDto(
        string Id,
        DateTimeOffset Timestamp,
        string Type,
        string Title,
        string? Detail,
        string? ServiceName,
        string Severity,
        bool IsSimulated);

    public sealed record AnnotationSourceSummary(
        string Source,
        int Count,
        bool IsSimulated,
        string? SimulatedNote);

    public sealed record Response(
        IReadOnlyList<AnnotationDto> Annotations,
        IReadOnlyList<AnnotationSourceSummary> Sources,
        DateTimeOffset From,
        DateTimeOffset To,
        int TotalCount);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.From).LessThan(x => x.To)
                .WithMessage("'From' must be before 'To'.");
            RuleFor(x => x.To).GreaterThan(x => x.From);
            RuleFor(x => x.MaxPerSource).InclusiveBetween(1, 200);
        }
    }

    public sealed class Handler(
        IIncidentModule incidentModule,
        IChangeIntelligenceModule changeIntelligenceModule,
        IRulesetGovernanceModule rulesetGovernanceModule,
        ILogger<Handler> logger) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var annotations = new List<AnnotationDto>();
            var sources = new List<AnnotationSourceSummary>();

            // — Incidents source
            try
            {
                var incidents = await incidentModule.GetRecentIncidentsAsync(
                    request.TenantId, request.From, request.To, request.MaxPerSource, cancellationToken);

                var incidentAnnotations = incidents
                    .Where(i => MatchesServiceFilter(i.ServiceName, request.ServiceNames))
                    .Select(i => new AnnotationDto(
                        Id: $"incident-{i.Id}",
                        Timestamp: i.DetectedAt,
                        Type: "incident.opened",
                        Title: i.Title,
                        Detail: $"Severity: {i.Severity} — Status: {i.Status}",
                        ServiceName: i.ServiceName,
                        Severity: MapIncidentSeverity(i.Severity),
                        IsSimulated: false))
                    .ToList();

                annotations.AddRange(incidentAnnotations);
                sources.Add(new AnnotationSourceSummary("incidents", incidentAnnotations.Count, false, null));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch incident annotations; using simulated fallback");
                sources.Add(new AnnotationSourceSummary("incidents", 0, true, "OperationalIntelligence module unavailable"));
            }

            // — Releases/changes source
            try
            {
                var releases = await changeIntelligenceModule.GetReleasesInWindowAsync(
                    request.From, request.To, request.MaxPerSource, cancellationToken);

                var releaseAnnotations = releases
                    .Where(r => MatchesServiceFilter(r.ServiceName, request.ServiceNames))
                    .Select(r => new AnnotationDto(
                        Id: $"release-{r.ReleaseId}",
                        Timestamp: r.CreatedAt,
                        Type: "change.release",
                        Title: $"Release {r.ServiceName} {r.Version}",
                        Detail: $"Environment: {r.Environment} — Status: {r.Status}",
                        ServiceName: r.ServiceName,
                        Severity: "info",
                        IsSimulated: false))
                    .ToList();

                annotations.AddRange(releaseAnnotations);
                sources.Add(new AnnotationSourceSummary("changes", releaseAnnotations.Count, false, null));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch release annotations; using simulated fallback");
                sources.Add(new AnnotationSourceSummary("changes", 0, true, "ChangeIntelligence module unavailable"));
            }

            // — Policy violations source
            try
            {
                var violations = await rulesetGovernanceModule.GetRecentViolationsAsync(
                    request.From, request.To, request.MaxPerSource, cancellationToken);

                var violationAnnotations = violations
                    .Select(v => new AnnotationDto(
                        Id: $"violation-{v.LintResultId}",
                        Timestamp: v.ExecutedAt,
                        Type: "policy.violation",
                        Title: $"Contract violations detected ({v.TotalFindings} findings)",
                        Detail: $"Lint score: {v.Score:F1} — Release: {v.ReleaseId}",
                        ServiceName: null,
                        Severity: v.Score < 50 ? "critical" : "warning",
                        IsSimulated: false))
                    .ToList();

                annotations.AddRange(violationAnnotations);
                sources.Add(new AnnotationSourceSummary("policies", violationAnnotations.Count, false, null));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch policy violation annotations; using simulated fallback");
                sources.Add(new AnnotationSourceSummary("policies", 0, true, "RulesetGovernance module unavailable"));
            }

            // — Contracts source (not yet bridged — honest gap)
            sources.Add(new AnnotationSourceSummary("contracts", 0, true, "Catalog contract events bridge pending"));

            var sorted = annotations
                .OrderByDescending(a => a.Timestamp)
                .ToList();

            return Result<Response>.Success(new Response(
                Annotations: sorted,
                Sources: sources,
                From: request.From,
                To: request.To,
                TotalCount: sorted.Count));
        }

        private static bool MatchesServiceFilter(string? serviceName, IReadOnlyList<string>? filter) =>
            filter is null or { Count: 0 } ||
            (serviceName is not null && filter.Contains(serviceName, StringComparer.OrdinalIgnoreCase));

        private static string MapIncidentSeverity(string severity) => severity.ToUpperInvariant() switch
        {
            "CRITICAL" or "P1" => "critical",
            "HIGH" or "P2" => "warning",
            _ => "info"
        };
    }
}
