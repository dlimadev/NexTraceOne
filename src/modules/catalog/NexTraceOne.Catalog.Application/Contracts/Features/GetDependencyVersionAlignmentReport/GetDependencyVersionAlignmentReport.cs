using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetDependencyVersionAlignmentReport;

/// <summary>
/// Feature: GetDependencyVersionAlignmentReport — alinhamento de versões de dependências.
/// Wave AR.3 — Service Topology Intelligence &amp; Dependency Mapping.
/// </summary>
public static class GetDependencyVersionAlignmentReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int MajorDriftThreshold = 3,
        int CriticalServiceCount = 2) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(x => x.MajorDriftThreshold).GreaterThan(1);
            RuleFor(x => x.CriticalServiceCount).GreaterThan(0);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum AlignmentTier { Aligned, MinorDrift, MajorDrift, SecurityRisk }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ComponentAlignmentDetail(
        string ComponentName,
        int VersionSpread,
        IReadOnlyList<string> VersionsInUse,
        string LatestAvailable,
        string OldestInUse,
        IReadOnlyDictionary<string, IReadOnlyList<string>> ServicesByVersion,
        IReadOnlyList<string> ServicesOnOldestVersion,
        bool HasSecurityImplications,
        AlignmentTier AlignmentTier);

    public sealed record AlignmentUpgradeEntry(
        string ComponentName,
        string TargetVersion,
        IReadOnlyList<string> ServicesNeedingUpgrade);

    public sealed record CrossTeamInconsistency(
        string ComponentName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> TeamVersionMap);

    public sealed record CriticalAlignmentGap(
        string ComponentName,
        int AffectedServiceCount,
        IReadOnlyList<string> AffectedServiceIds);

    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        int ComponentsAnalyzed,
        double TenantAlignmentScore,
        IReadOnlyList<ComponentAlignmentDetail> ComponentDetails,
        IReadOnlyList<AlignmentUpgradeEntry> AlignmentUpgradeMap,
        IReadOnlyList<CrossTeamInconsistency> CrossTeamInconsistencies,
        IReadOnlyList<CriticalAlignmentGap> CriticalAlignmentGaps);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IDependencyVersionAlignmentReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;

            var entries = await reader.ListComponentVersionsByTenantAsync(request.TenantId, cancellationToken);

            if (entries.Count == 0)
            {
                var emptyReport = new Report(
                    now, request.TenantId, request.LookbackDays,
                    0, 100.0, [], [], [], []);
                return Result<Report>.Success(emptyReport);
            }

            var grouped = entries.GroupBy(e => e.ComponentName).ToList();

            var componentDetails = grouped.Select(g =>
                BuildComponentDetail(g.Key, g.ToList(), request.MajorDriftThreshold)).ToList();

            // TenantAlignmentScore: % of components with Aligned or MinorDrift
            int alignedCount = componentDetails.Count(c =>
                c.AlignmentTier == AlignmentTier.Aligned || c.AlignmentTier == AlignmentTier.MinorDrift);
            double alignmentScore = componentDetails.Count == 0 ? 100.0
                : Math.Round((double)alignedCount / componentDetails.Count * 100.0, 2);

            // AlignmentUpgradeMap: components with drift
            var upgradeMap = componentDetails
                .Where(c => c.AlignmentTier != AlignmentTier.Aligned)
                .Select(c =>
                {
                    var servicesNeedingUpgrade = c.ServicesByVersion
                        .Where(kv => kv.Key != c.LatestAvailable)
                        .SelectMany(kv => kv.Value)
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList();
                    return new AlignmentUpgradeEntry(c.ComponentName, c.LatestAvailable, servicesNeedingUpgrade);
                })
                .Where(u => u.ServicesNeedingUpgrade.Count > 0)
                .ToList();

            // CrossTeamInconsistencies: multiple teams using different versions of same component
            var crossTeamInconsistencies = new List<CrossTeamInconsistency>();
            foreach (var g in grouped)
            {
                var teamVersions = g
                    .GroupBy(e => e.TeamId)
                    .ToDictionary(
                        tg => tg.Key,
                        tg => (IReadOnlyList<string>)tg.Select(e => e.ComponentVersion).Distinct().OrderBy(v => v).ToList());

                var distinctVersionsAcrossTeams = teamVersions.Values.SelectMany(v => v).Distinct().Count();
                if (teamVersions.Count > 1 && distinctVersionsAcrossTeams > 1)
                    crossTeamInconsistencies.Add(new CrossTeamInconsistency(g.Key, teamVersions));
            }

            // CriticalAlignmentGaps: SecurityRisk + >= CriticalServiceCount affected services
            var criticalGaps = componentDetails
                .Where(c => c.AlignmentTier == AlignmentTier.SecurityRisk)
                .Select(c =>
                {
                    var affectedServices = c.ServicesByVersion.Values.SelectMany(s => s).Distinct().ToList();
                    return new CriticalAlignmentGap(c.ComponentName, affectedServices.Count, affectedServices);
                })
                .Where(g => g.AffectedServiceCount >= request.CriticalServiceCount)
                .ToList();

            var report = new Report(
                now, request.TenantId, request.LookbackDays,
                componentDetails.Count, alignmentScore,
                componentDetails, upgradeMap, crossTeamInconsistencies, criticalGaps);

            return Result<Report>.Success(report);
        }

        private static ComponentAlignmentDetail BuildComponentDetail(
            string componentName,
            IReadOnlyList<IDependencyVersionAlignmentReader.ComponentVersionEntry> entries,
            int majorDriftThreshold)
        {
            var versionGroups = entries
                .GroupBy(e => e.ComponentVersion)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<string>)g.Select(e => e.ServiceId).Distinct().OrderBy(s => s).ToList());

            var versionsInUse = versionGroups.Keys.OrderBy(v => v).ToList();
            int versionSpread = versionsInUse.Count;
            string latestAvailable = versionsInUse.Max() ?? string.Empty;
            string oldestInUse = versionsInUse.Min() ?? string.Empty;

            bool hasSecurityImplications = entries.Any(e => e.HasKnownCve);

            var servicesOnOldest = versionGroups.GetValueOrDefault(oldestInUse, []);

            AlignmentTier tier;
            if (hasSecurityImplications && versionSpread > 1)
                tier = AlignmentTier.SecurityRisk;
            else if (versionSpread <= 1)
                tier = AlignmentTier.Aligned;
            else if (versionSpread > majorDriftThreshold)
                tier = AlignmentTier.MajorDrift;
            else
                tier = AlignmentTier.MinorDrift;

            return new ComponentAlignmentDetail(
                componentName, versionSpread, versionsInUse,
                latestAvailable, oldestInUse, versionGroups,
                servicesOnOldest, hasSecurityImplications, tier);
        }
    }
}
