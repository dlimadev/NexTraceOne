using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetDashboardAnnotations;

/// <summary>
/// Feature: GetDashboardAnnotations — agrega eventos de anotação para sobreposição
/// em widgets de séries temporais de um dashboard.
///
/// Fontes de anotação:
/// — Changes (releases, rollouts)
/// — Incidents (abertura, resolução)
/// — Contract Breaking Changes (detection events)
/// — Policy Violations
///
/// As fontes cross-módulo que ainda não têm bridge real retornam anotações simuladas
/// com SimulatedNote (honest gap pattern).
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

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Honest gap: cross-module annotation sources (ChangeGovernance, OperationalIntelligence,
            // Catalog contract events) are not yet bridged into the Governance bounded context.
            // We return representative simulated annotations so the frontend overlay is functional
            // and design can be validated before real bridges are wired.
            var annotations = BuildSimulatedAnnotations(request);
            var sources = BuildSourceSummaries(annotations);

            return Task.FromResult(Result<Response>.Success(new Response(
                Annotations: annotations,
                Sources: sources,
                From: request.From,
                To: request.To,
                TotalCount: annotations.Count)));
        }

        private static List<AnnotationDto> BuildSimulatedAnnotations(Query req)
        {
            var mid = req.From + (req.To - req.From) / 2;
            var quarter = (req.To - req.From) / 4;

            // Filter by service names if provided
            bool Matches(string? svc) =>
                req.ServiceNames is null or { Count: 0 } ||
                (svc is not null && req.ServiceNames.Contains(svc, StringComparer.OrdinalIgnoreCase));

            var all = new List<AnnotationDto>
            {
                new(
                    Id: "ann-change-1",
                    Timestamp: req.From + quarter,
                    Type: "change.release",
                    Title: "Release payment-service v2.3.1",
                    Detail: "Deployed to production — confidence 94",
                    ServiceName: "payment-service",
                    Severity: "info",
                    IsSimulated: true),

                new(
                    Id: "ann-incident-1",
                    Timestamp: mid,
                    Type: "incident.opened",
                    Title: "P1 Incident — payment-service latency spike",
                    Detail: "Resolved in 18 minutes",
                    ServiceName: "payment-service",
                    Severity: "critical",
                    IsSimulated: true),

                new(
                    Id: "ann-contract-1",
                    Timestamp: mid + quarter,
                    Type: "contract.breaking_change",
                    Title: "Breaking change detected — user-service REST v1.5.0",
                    Detail: "Field 'email' removed from /users/{id} response",
                    ServiceName: "user-service",
                    Severity: "warning",
                    IsSimulated: true),

                new(
                    Id: "ann-policy-1",
                    Timestamp: req.To - (quarter / 2),
                    Type: "policy.violation",
                    Title: "Policy violation — analytics-service missing runbook",
                    Detail: "Tier Standard requires runbook for every Critical SLO",
                    ServiceName: "analytics-service",
                    Severity: "warning",
                    IsSimulated: true)
            };

            return all
                .Where(a => Matches(a.ServiceName) && a.Timestamp >= req.From && a.Timestamp <= req.To)
                .Take(req.MaxPerSource * 4)
                .ToList();
        }

        private static List<AnnotationSourceSummary> BuildSourceSummaries(
            IReadOnlyList<AnnotationDto> annotations)
        {
            var byType = annotations
                .GroupBy(a => a.Type.Split('.')[0])
                .ToDictionary(g => g.Key, g => g.Count());

            return
            [
                new("changes",   byType.GetValueOrDefault("change",   0),   true, "Simulated — ChangeGovernance bridge pending"),
                new("incidents", byType.GetValueOrDefault("incident", 0),   true, "Simulated — OperationalIntelligence bridge pending"),
                new("contracts", byType.GetValueOrDefault("contract", 0),   true, "Simulated — Catalog bridge pending"),
                new("policies",  byType.GetValueOrDefault("policy",   0),   true, "Simulated — PolicyEngine bridge pending")
            ];
        }
    }
}
