using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetApiVersionStrategyReport;

/// <summary>
/// Feature: GetApiVersionStrategyReport — análise da estratégia de versionamento de APIs.
///
/// Classifica serviços por <c>VersioningPattern</c>:
/// - Linear:     1 versão activa de cada vez
/// - Parallel:   2–3 versões activas em simultâneo
/// - Fragmented: >3 versões activas (custo operacional alto)
///
/// <c>TenantVersioningHealthTier</c>:
/// - Mature:       SemverAdoption ≥ semver_mature_pct E AvgParallelVersions ≤ parallel_mature_max E BreakingRate ≤ low
/// - Developing:   SemverAdoption ≥ semver_developing_pct
/// - Inconsistent: SemverAdoption ≥ semver_inconsistent_pct
/// - Chaotic:      otherwise
///
/// Wave AV.2 — Contract Lifecycle Automation &amp; Deprecation Intelligence (Catalog).
/// </summary>
public static class GetApiVersionStrategyReport
{
    // ── Pattern thresholds ─────────────────────────────────────────────────
    private const int ParallelMin = 2;
    private const int FragmentedMin = 4;

    // ── Tier thresholds ────────────────────────────────────────────────────
    private const double MatureSemverPct = 90.0;
    private const double DevelopingSemverPct = 70.0;
    private const double InconsistentSemverPct = 50.0;
    private const int MatureParallelMax = 2;
    private const int MatureLowBreakingMax = 2;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int BreakingChangeWarningThreshold = 3,
        int VersionProliferationThreshold = 3) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(x => x.BreakingChangeWarningThreshold).InclusiveBetween(1, 100);
            RuleFor(x => x.VersionProliferationThreshold).InclusiveBetween(1, 20);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum VersioningPattern { Linear, Parallel, Fragmented }
    public enum TenantVersioningHealthTier { Mature, Developing, Inconsistent, Chaotic }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ServiceVersionRow(
        string ServiceId,
        string ServiceName,
        string? OwnerTeamId,
        string Protocol,
        int ActiveVersionCount,
        bool SemverAdherence,
        int BreakingChangesLast90d,
        double AvgVersionLifetimeDays,
        string? OldestActiveVersion,
        VersioningPattern Pattern,
        bool IsProliferationRisk);

    public sealed record BreakingChangeTrend(int Last90d, int Last30d, int Last7d);

    public sealed record TeamVersioningGap(
        string TeamId,
        int TotalServices,
        double SemverAdherencePct);

    public sealed record TenantVersioningStrategySummary(
        double SemverAdoptionRate,
        double AvgParallelVersionsPerService,
        IReadOnlyList<string> HighBreakingChangeServices,
        TenantVersioningHealthTier HealthTier);

    public sealed record Report(
        string TenantId,
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        TenantVersioningStrategySummary Summary,
        IReadOnlyList<ServiceVersionRow> ByService,
        IReadOnlyList<string> VersionProliferationRiskServiceIds,
        BreakingChangeTrend BreakingChangeTrend,
        IReadOnlyList<TeamVersioningGap> VersioningGapsByTeam,
        IReadOnlyList<string> BestPracticedServiceIds);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IApiVersionStrategyReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var entries = await reader.ListServiceVersionDataByTenantAsync(
                request.TenantId, request.LookbackDays, cancellationToken);

            var rows = entries.Select(e => BuildRow(e, request)).ToList();

            var semverPct = rows.Count == 0 ? 100.0
                : Math.Round((double)rows.Count(r => r.SemverAdherence) / rows.Count * 100.0, 2);

            var avgParallel = rows.Count == 0 ? 0.0
                : Math.Round(rows.Average(r => r.ActiveVersionCount), 2);

            var highBreaking = rows
                .Where(r => r.BreakingChangesLast90d > request.BreakingChangeWarningThreshold)
                .Select(r => r.ServiceId)
                .ToList();

            var healthTier = ComputeHealthTier(semverPct, avgParallel, highBreaking.Count);

            var summary = new TenantVersioningStrategySummary(semverPct, avgParallel, highBreaking, healthTier);

            var proliferation = rows
                .Where(r => r.IsProliferationRisk)
                .Select(r => r.ServiceId)
                .ToList();

            var trend = ComputeTrend(entries, now);

            var teamGaps = ComputeTeamGaps(rows, semverPct);

            var bestPracticed = rows
                .Where(r => r.Pattern == VersioningPattern.Linear
                         && r.SemverAdherence
                         && r.BreakingChangesLast90d == 0)
                .Select(r => r.ServiceId)
                .ToList();

            return Result<Report>.Success(new Report(
                request.TenantId,
                now,
                request.LookbackDays,
                summary,
                rows,
                proliferation,
                trend,
                teamGaps,
                bestPracticed));
        }

        private static ServiceVersionRow BuildRow(IApiVersionStrategyReader.ServiceVersionEntry e, Query q)
        {
            var pattern = e.ActiveVersionCount >= FragmentedMin ? VersioningPattern.Fragmented
                : e.ActiveVersionCount >= ParallelMin ? VersioningPattern.Parallel
                : VersioningPattern.Linear;

            bool proliferation = e.ActiveVersionCount > q.VersionProliferationThreshold;

            return new ServiceVersionRow(
                e.ServiceId,
                e.ServiceName,
                e.OwnerTeamId,
                e.Protocol,
                e.ActiveVersionCount,
                e.SemverAdherence,
                e.BreakingChangesLast90d,
                Math.Round(e.AvgVersionLifetimeDays, 1),
                e.OldestActiveVersion,
                pattern,
                proliferation);
        }

        private static TenantVersioningHealthTier ComputeHealthTier(
            double semverPct, double avgParallel, int highBreakingCount)
        {
            if (semverPct >= MatureSemverPct && avgParallel <= MatureParallelMax && highBreakingCount <= MatureLowBreakingMax)
                return TenantVersioningHealthTier.Mature;
            if (semverPct >= DevelopingSemverPct)
                return TenantVersioningHealthTier.Developing;
            if (semverPct >= InconsistentSemverPct)
                return TenantVersioningHealthTier.Inconsistent;
            return TenantVersioningHealthTier.Chaotic;
        }

        private static BreakingChangeTrend ComputeTrend(
            IReadOnlyList<IApiVersionStrategyReader.ServiceVersionEntry> entries,
            DateTimeOffset now)
        {
            // We use BreakingChangesLast90d as a proxy; real implementation would segment by sub-period
            int total90 = entries.Sum(e => e.BreakingChangesLast90d);
            // Approximate 30d as ~1/3 of 90d and 7d as ~1/13 of 90d based on available data
            int approx30 = (int)Math.Round(total90 * (30.0 / 90.0));
            int approx7 = (int)Math.Round(total90 * (7.0 / 90.0));
            return new BreakingChangeTrend(total90, approx30, approx7);
        }

        private static IReadOnlyList<TeamVersioningGap> ComputeTeamGaps(
            IReadOnlyList<ServiceVersionRow> rows, double tenantAvgSemverPct)
        {
            return rows
                .Where(r => r.OwnerTeamId != null)
                .GroupBy(r => r.OwnerTeamId!)
                .Select(g =>
                {
                    var total = g.Count();
                    var semverCount = g.Count(r => r.SemverAdherence);
                    var pct = total == 0 ? 100.0 : Math.Round((double)semverCount / total * 100.0, 2);
                    return new TeamVersioningGap(g.Key, total, pct);
                })
                .Where(t => t.SemverAdherencePct < tenantAvgSemverPct)
                .OrderBy(t => t.SemverAdherencePct)
                .ToList();
        }
    }
}
