using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetSecurityIncidentCorrelationReport;

/// <summary>
/// Feature: GetSecurityIncidentCorrelationReport — relatório de correlação de incidentes de segurança com CVEs.
///
/// Classifica incidentes por <c>SecurityIncidentCorrelationRisk</c>:
/// - Strong:   sinais ≥ 2 E CriticalCvePresentAtTime E VulnerableComponentIntroducedRecently
/// - Likely:   sinais ≥ 2
/// - Possible: sinais == 1
/// - None:     sem sinais
///
/// Wave AX.3 — Security Posture &amp; Vulnerability Intelligence.
/// </summary>
public static class GetSecurityIncidentCorrelationReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int CorrelationWindowHours = 72) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(7, 365);
            RuleFor(x => x.CorrelationWindowHours).InclusiveBetween(1, 168);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum SecurityIncidentCorrelationRisk { None, Possible, Likely, Strong }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record IncidentCorrelationEntry(
        Guid IncidentId,
        string ServiceId,
        string ServiceName,
        DateTimeOffset OccurredAt,
        int ActiveCveCountAtTime,
        bool CriticalCvePresentAtTime,
        bool VulnerableComponentIntroducedRecently,
        IReadOnlyList<string> CorrelationSignals,
        SecurityIncidentCorrelationRisk Risk);

    public sealed record TenantSecurityIncidentSummary(
        int SecurityIncidentCount,
        int WithActiveUnpatchedCVE,
        int StrongCorrelationIncidents,
        decimal TenantCVEIncidentCorrelationRate);

    public sealed record Report(
        string TenantId,
        DateTimeOffset From,
        DateTimeOffset To,
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        IReadOnlyList<IncidentCorrelationEntry> ByIncident,
        TenantSecurityIncidentSummary TenantSecurityIncidentSummary,
        IReadOnlyList<string> CVEsWithIncidentCorrelation,
        IReadOnlyList<string> ComponentsIntroducedBeforeIncident,
        int RiskReductionOpportunity);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        ISecurityIncidentCorrelationReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var from = now.AddDays(-request.LookbackDays);
            var to = now;

            var incidents = await reader.ListSecurityIncidentsByTenantAsync(request.TenantId, from, to, cancellationToken);

            var correlations = incidents.Select(BuildEntry).ToList();

            int total = correlations.Count;
            int withUnpatched = correlations.Count(e => e.CriticalCvePresentAtTime);
            int strongCount = correlations.Count(e => e.Risk == SecurityIncidentCorrelationRisk.Strong);

            decimal correlationRate = total == 0
                ? 0m
                : Math.Round((decimal)correlations.Count(e => e.CorrelationSignals.Count > 0) / total * 100m, 2);

            var summary = new TenantSecurityIncidentSummary(total, withUnpatched, strongCount, correlationRate);

            var allComponents = correlations
                .SelectMany(e => e.VulnerableComponentIntroducedRecently
                    ? incidents.First(i => i.IncidentId == e.IncidentId).IntroducedVulnerableComponents
                    : incidents.First(i => i.IncidentId == e.IncidentId).IntroducedVulnerableComponents)
                .ToList();

            var componentsByIncident = correlations
                .SelectMany(e => incidents
                    .First(i => i.IncidentId == e.IncidentId)
                    .IntroducedVulnerableComponents
                    .Select(c => new { Component = c, e.IncidentId }))
                .GroupBy(x => x.Component)
                .ToList();

            var cvesWithCorrelation = componentsByIncident
                .Where(g => g.Select(x => x.IncidentId).Distinct().Count() > 1)
                .Select(g => g.Key)
                .Distinct()
                .ToList();

            var allDistinctComponents = componentsByIncident
                .Select(g => g.Key)
                .Distinct()
                .ToList();

            int riskReduction = correlations.Count(e =>
                e.Risk == SecurityIncidentCorrelationRisk.Likely ||
                e.Risk == SecurityIncidentCorrelationRisk.Strong);

            return Result<Report>.Success(new Report(
                request.TenantId,
                from,
                to,
                now,
                request.LookbackDays,
                correlations,
                summary,
                cvesWithCorrelation,
                allDistinctComponents,
                riskReduction));
        }

        private static IncidentCorrelationEntry BuildEntry(ISecurityIncidentCorrelationReader.SecurityIncidentEntry e)
        {
            var signals = new List<string>();

            if (e.CriticalCvePresentAtTime)
                signals.Add("unpatched_critical_cve_present");
            if (e.VulnerableComponentIntroducedRecently)
                signals.Add("vulnerable_component_introduced_recently");
            if (e.ActiveCveCountAtTime >= 5)
                signals.Add("elevated_cve_exposure");

            SecurityIncidentCorrelationRisk risk;
            if (signals.Count >= 2 && e.CriticalCvePresentAtTime && e.VulnerableComponentIntroducedRecently)
                risk = SecurityIncidentCorrelationRisk.Strong;
            else if (signals.Count >= 2)
                risk = SecurityIncidentCorrelationRisk.Likely;
            else if (signals.Count == 1)
                risk = SecurityIncidentCorrelationRisk.Possible;
            else
                risk = SecurityIncidentCorrelationRisk.None;

            return new IncidentCorrelationEntry(
                e.IncidentId,
                e.ServiceId,
                e.ServiceName,
                e.OccurredAt,
                e.ActiveCveCountAtTime,
                e.CriticalCvePresentAtTime,
                e.VulnerableComponentIntroducedRecently,
                signals,
                risk);
        }
    }
}
