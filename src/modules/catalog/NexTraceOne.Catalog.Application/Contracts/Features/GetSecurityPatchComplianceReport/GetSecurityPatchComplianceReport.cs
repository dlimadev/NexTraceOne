using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetSecurityPatchComplianceReport;

/// <summary>
/// Feature: GetSecurityPatchComplianceReport — relatório de conformidade de patching de segurança.
///
/// Clasifica serviços por <c>PatchComplianceTier</c>:
/// - AtRisk:       CriticalPatchBacklogCount &gt; 0
/// - Compliant:    CriticalWithinSlaRate ≥ CompliantCriticalRate E HighWithinSlaRate ≥ 90
/// - Partial:      CriticalWithinSlaRate ≥ 70 OU CriticalPatchBacklogCount == 0
/// - NonCompliant: caso contrário
///
/// SLAs padrão: Critical=7d, High=30d, Medium=90d, Low=180d.
///
/// Wave AX.2 — Security Posture &amp; Vulnerability Intelligence.
/// </summary>
public static class GetSecurityPatchComplianceReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int PatchSlaCriticalDays = 7,
        int PatchSlaHighDays = 30,
        int PatchSlaMediumDays = 90,
        int PatchSlaLowDays = 180,
        decimal CompliantCriticalRate = 95m) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(7, 365);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum PatchComplianceTier { Compliant, Partial, NonCompliant, AtRisk }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record PatchComplianceRateBySeverity(
        decimal CriticalWithinSlaRate,
        decimal HighWithinSlaRate,
        decimal MediumWithinSlaRate,
        decimal LowWithinSlaRate);

    public sealed record SlaBreachEntry(
        string ServiceId,
        string ServiceName,
        string CveId,
        string Severity,
        DateTimeOffset DiscoveredAt,
        DateTimeOffset RemediatedAt,
        double DaysToRemediate,
        int SlaDays);

    public sealed record SlowPatchingTeamEntry(
        string TeamName,
        double AvgPatchTimeDays,
        double MedianAvgPatchTimeDays,
        int HighOrCriticalCveCount);

    public sealed record TenantPatchComplianceSummary(
        decimal OverallPatchComplianceRate,
        int CriticalPatchBacklogCount,
        decimal TenantPatchComplianceScore,
        PatchComplianceTier Tier);

    public sealed record Report(
        string TenantId,
        DateTimeOffset From,
        DateTimeOffset To,
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        PatchComplianceRateBySeverity PatchComplianceRateBySeverity,
        TenantPatchComplianceSummary TenantPatchComplianceSummary,
        IReadOnlyList<SlaBreachEntry> SLABreaches,
        IReadOnlyList<SlowPatchingTeamEntry> SlowPatchingTeams,
        IReadOnlyList<PatchComplianceTier> PatchComplianceTrend);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        ISecurityPatchComplianceReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var from = now.AddDays(-request.LookbackDays);
            var to = now;

            var entries = await reader.ListByTenantAsync(request.TenantId, from, to, cancellationToken);

            var rates = ComputeRates(entries, request);
            int criticalBacklog = ComputeCriticalBacklog(entries, now, request.PatchSlaCriticalDays);
            decimal overallRate = ComputeOverallRate(entries, request);
            var slaBreaches = ComputeSlaBreaches(entries, request);
            var slowTeams = ComputeSlowPatchingTeams(entries);
            var trend = ComputeTrend(entries, now, rates, request);
            var tier = ComputeTier(criticalBacklog, rates.CriticalWithinSlaRate, rates.HighWithinSlaRate, request.CompliantCriticalRate);

            var summary = new TenantPatchComplianceSummary(overallRate, criticalBacklog, overallRate, tier);

            return Result<Report>.Success(new Report(
                request.TenantId,
                from,
                to,
                now,
                request.LookbackDays,
                rates,
                summary,
                slaBreaches,
                slowTeams,
                trend));
        }

        private static int GetSla(string severity, Query q) =>
            severity.ToUpperInvariant() switch
            {
                "CRITICAL" => q.PatchSlaCriticalDays,
                "HIGH" => q.PatchSlaHighDays,
                "MEDIUM" => q.PatchSlaMediumDays,
                "LOW" => q.PatchSlaLowDays,
                _ => q.PatchSlaLowDays
            };

        private static PatchComplianceRateBySeverity ComputeRates(
            IReadOnlyList<ISecurityPatchComplianceReader.PatchComplianceEntry> entries,
            Query q)
        {
            var all = entries.SelectMany(e => e.RemediatedCves).ToList();

            static decimal Rate(IEnumerable<ISecurityPatchComplianceReader.RemediatedCve> cvesOfSeverity, int sla)
            {
                var list = cvesOfSeverity.ToList();
                if (list.Count == 0) return 0m;
                int within = list.Count(c => (c.RemediatedAt - c.DiscoveredAt).TotalDays <= sla);
                return Math.Round((decimal)within / list.Count * 100m, 2);
            }

            return new PatchComplianceRateBySeverity(
                Rate(all.Where(c => c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)), q.PatchSlaCriticalDays),
                Rate(all.Where(c => c.Severity.Equals("High", StringComparison.OrdinalIgnoreCase)), q.PatchSlaHighDays),
                Rate(all.Where(c => c.Severity.Equals("Medium", StringComparison.OrdinalIgnoreCase)), q.PatchSlaMediumDays),
                Rate(all.Where(c => c.Severity.Equals("Low", StringComparison.OrdinalIgnoreCase)), q.PatchSlaLowDays));
        }

        private static decimal ComputeOverallRate(
            IReadOnlyList<ISecurityPatchComplianceReader.PatchComplianceEntry> entries,
            Query q)
        {
            var all = entries.SelectMany(e => e.RemediatedCves).ToList();
            if (all.Count == 0) return 0m;
            int within = all.Count(c => (c.RemediatedAt - c.DiscoveredAt).TotalDays <= GetSla(c.Severity, q));
            return Math.Round((decimal)within / all.Count * 100m, 2);
        }

        private static int ComputeCriticalBacklog(
            IReadOnlyList<ISecurityPatchComplianceReader.PatchComplianceEntry> entries,
            DateTimeOffset now,
            int slaDays) =>
            entries
                .SelectMany(e => e.ActiveCves)
                .Count(c => c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)
                         && (now - c.DiscoveredAt).TotalDays > slaDays);

        private static IReadOnlyList<SlaBreachEntry> ComputeSlaBreaches(
            IReadOnlyList<ISecurityPatchComplianceReader.PatchComplianceEntry> entries,
            Query q)
        {
            return entries
                .SelectMany(e => e.RemediatedCves.Select(c => (Entry: e, Cve: c)))
                .Select(x =>
                {
                    double days = (x.Cve.RemediatedAt - x.Cve.DiscoveredAt).TotalDays;
                    int sla = GetSla(x.Cve.Severity, q);
                    return new { x.Entry, x.Cve, Days = days, Sla = sla, WithinSla = days <= sla };
                })
                .Where(x => !x.WithinSla)
                .OrderByDescending(x => x.Days)
                .Take(20)
                .Select(x => new SlaBreachEntry(
                    x.Entry.ServiceId,
                    x.Entry.ServiceName,
                    x.Cve.CveId,
                    x.Cve.Severity,
                    x.Cve.DiscoveredAt,
                    x.Cve.RemediatedAt,
                    x.Days,
                    x.Sla))
                .ToList();
        }

        private static IReadOnlyList<SlowPatchingTeamEntry> ComputeSlowPatchingTeams(
            IReadOnlyList<ISecurityPatchComplianceReader.PatchComplianceEntry> entries)
        {
            var teamGroups = entries
                .SelectMany(e => e.RemediatedCves.Select(c => (TeamName: e.TeamName, Days: (c.RemediatedAt - c.DiscoveredAt).TotalDays)))
                .GroupBy(x => x.TeamName)
                .Select(g => new { TeamName = g.Key, AvgDays = g.Average(x => x.Days) })
                .ToList();

            if (teamGroups.Count == 0) return [];

            var sortedAvgs = teamGroups.Select(t => t.AvgDays).OrderBy(d => d).ToList();
            double median = sortedAvgs.Count % 2 == 0
                ? (sortedAvgs[sortedAvgs.Count / 2 - 1] + sortedAvgs[sortedAvgs.Count / 2]) / 2.0
                : sortedAvgs[sortedAvgs.Count / 2];

            var teamsWithHighOrCritical = entries
                .Where(e => e.RemediatedCves.Any(c =>
                                c.Severity.Equals("High", StringComparison.OrdinalIgnoreCase) ||
                                c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase))
                         || e.ActiveCves.Any(c =>
                                c.Severity.Equals("High", StringComparison.OrdinalIgnoreCase) ||
                                c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)))
                .Select(e => e.TeamName)
                .ToHashSet();

            return teamGroups
                .Where(t => t.AvgDays > median && teamsWithHighOrCritical.Contains(t.TeamName))
                .Select(t =>
                {
                    int highOrCriticalCount = entries
                        .Where(e => e.TeamName == t.TeamName)
                        .Sum(e =>
                            e.RemediatedCves.Count(c =>
                                c.Severity.Equals("High", StringComparison.OrdinalIgnoreCase) ||
                                c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)) +
                            e.ActiveCves.Count(c =>
                                c.Severity.Equals("High", StringComparison.OrdinalIgnoreCase) ||
                                c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)));
                    return new SlowPatchingTeamEntry(t.TeamName, t.AvgDays, median, highOrCriticalCount);
                })
                .ToList();
        }

        private static IReadOnlyList<PatchComplianceTier> ComputeTrend(
            IReadOnlyList<ISecurityPatchComplianceReader.PatchComplianceEntry> entries,
            DateTimeOffset now,
            PatchComplianceRateBySeverity rates,
            Query q)
        {
            var trend = new List<PatchComplianceTier>(4);

            for (int w = 0; w < 4; w++)
            {
                var boundary = now.AddDays(-w * 7);
                int criticalActiveAtBoundary = entries
                    .SelectMany(e => e.ActiveCves)
                    .Count(c => c.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)
                             && c.DiscoveredAt <= boundary);

                PatchComplianceTier t;
                if (criticalActiveAtBoundary > 0)
                    t = PatchComplianceTier.AtRisk;
                else if (rates.CriticalWithinSlaRate >= q.CompliantCriticalRate && rates.HighWithinSlaRate >= 90m)
                    t = PatchComplianceTier.Compliant;
                else if (rates.CriticalWithinSlaRate >= 70m)
                    t = PatchComplianceTier.Partial;
                else
                    t = PatchComplianceTier.NonCompliant;

                trend.Add(t);
            }

            return trend;
        }

        private static PatchComplianceTier ComputeTier(
            int criticalBacklog,
            decimal criticalRate,
            decimal highRate,
            decimal compliantCriticalRate)
        {
            if (criticalBacklog > 0)
                return PatchComplianceTier.AtRisk;
            if (criticalRate >= compliantCriticalRate && highRate >= 90m)
                return PatchComplianceTier.Compliant;
            if (criticalRate >= 70m || criticalBacklog == 0)
                return PatchComplianceTier.Partial;
            return PatchComplianceTier.NonCompliant;
        }
    }
}
