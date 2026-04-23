using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetDocumentationHealthReport;

/// <summary>
/// Feature: GetDocumentationHealthReport — relatório de saúde da documentação por serviço, equipa e tenant.
///
/// Classifica serviços por <c>TenantDocHealthTier</c>:
/// - Excellent: TenantDocHealthScore ≥ 80
/// - Good: TenantDocHealthScore ≥ 60
/// - Partial: TenantDocHealthScore ≥ 40
/// - Critical: caso contrário, ou serviços críticos/high sem runbook
///
/// <c>DocHealthScore</c> por serviço (0–100):
///   RunbookCoverage (35%) + ApiDocCoverage (30%) + ArchitectureDoc (15%) + Freshness (20%)
///
/// Wave AY.1 — Organizational Knowledge &amp; Documentation Intelligence.
/// </summary>
public static class GetDocumentationHealthReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int FreshnessDays = 180,
        int RunbookFreshnessDays = 90,
        string CriticalWithoutRunbookTiers = "Critical,High",
        int MaxBestDocumentedServices = 5) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.FreshnessDays).InclusiveBetween(7, 730);
            RuleFor(x => x.RunbookFreshnessDays).InclusiveBetween(7, 365);
            RuleFor(x => x.MaxBestDocumentedServices).InclusiveBetween(1, 20);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum RunbookCoverageStatus { Covered, Stale, Missing }
    public enum ApiDocCoverageStatus { Full, Partial, Absent }
    public enum DocFreshnessTier { Fresh, Aging, Stale, Critical }
    public enum TenantDocHealthTier { Excellent, Good, Partial, Critical }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ServiceDocHealthEntry(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string ServiceTier,
        string DomainName,
        RunbookCoverageStatus RunbookCoverage,
        ApiDocCoverageStatus ApiDocCoverage,
        bool ArchitectureDocPresence,
        bool OnboardingDocPresence,
        DocFreshnessTier DocFreshnessTier,
        decimal DocHealthScore);

    public sealed record TeamDocDebt(
        string TeamId,
        string TeamName,
        int StaleDocCount,
        int MissingRunbookCount);

    public sealed record TenantDocumentationHealthSummary(
        int ServicesWithRunbook,
        int ServicesWithStaleRunbook,
        int ServicesWithoutRunbook,
        decimal ApiContractsFullyDocumentedPct,
        TenantDocHealthTier TenantDocHealthTier,
        decimal TenantDocHealthScore);

    public sealed record Report(
        string TenantId,
        DateTimeOffset GeneratedAt,
        IReadOnlyList<ServiceDocHealthEntry> ByService,
        TenantDocumentationHealthSummary Summary,
        IReadOnlyList<ServiceDocHealthEntry> CriticalServicesWithoutRunbook,
        IReadOnlyList<TeamDocDebt> StaleDocsByTeam,
        int DocDebt,
        IReadOnlyList<ServiceDocHealthEntry> BestDocumentedServices);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IDocumentationHealthReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        private const decimal ExcellentThreshold = 80m;
        private const decimal GoodThreshold = 60m;
        private const decimal PartialThreshold = 40m;

        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var criticalTiers = request.CriticalWithoutRunbookTiers
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var entries = await reader.ListByTenantAsync(request.TenantId, cancellationToken);

            var serviceEntries = entries.Select(e => BuildEntry(e, now, request, criticalTiers)).ToList();

            var criticalWithoutRunbook = serviceEntries
                .Where(e => criticalTiers.Contains(e.ServiceTier) && e.RunbookCoverage != RunbookCoverageStatus.Covered)
                .OrderBy(e => e.DocHealthScore)
                .ToList();

            var staleDocsByTeam = BuildStaleDocsByTeam(serviceEntries);

            var docDebt = serviceEntries.Count(e =>
                e.RunbookCoverage != RunbookCoverageStatus.Covered ||
                e.ApiDocCoverage != ApiDocCoverageStatus.Full ||
                !e.ArchitectureDocPresence ||
                e.DocFreshnessTier is DocFreshnessTier.Stale or DocFreshnessTier.Critical);

            var summary = BuildSummary(serviceEntries, entries, criticalWithoutRunbook);

            var best = serviceEntries
                .OrderByDescending(e => e.DocHealthScore)
                .Take(request.MaxBestDocumentedServices)
                .ToList();

            return Result<Report>.Success(new Report(
                request.TenantId,
                now,
                serviceEntries,
                summary,
                criticalWithoutRunbook,
                staleDocsByTeam,
                docDebt,
                best));
        }

        private static ServiceDocHealthEntry BuildEntry(
            IDocumentationHealthReader.ServiceDocumentationEntry e,
            DateTimeOffset now,
            Query request,
            HashSet<string> criticalTiers)
        {
            var runbookCov = ComputeRunbookCoverage(e, now, request.RunbookFreshnessDays);
            var apiDocCov = ComputeApiDocCoverage(e);
            var freshnessTier = ComputeFreshnessTier(e, now, request.FreshnessDays, criticalTiers);
            var score = ComputeDocHealthScore(runbookCov, apiDocCov, e.HasArchitectureDocUrl, freshnessTier);

            return new ServiceDocHealthEntry(
                e.ServiceId, e.ServiceName, e.TeamName, e.ServiceTier, e.DomainName,
                runbookCov, apiDocCov, e.HasArchitectureDocUrl, e.HasOnboardingDocUrl,
                freshnessTier, score);
        }

        internal static RunbookCoverageStatus ComputeRunbookCoverage(
            IDocumentationHealthReader.ServiceDocumentationEntry e,
            DateTimeOffset now,
            int freshnessMaxDays)
        {
            if (!e.HasRunbookUrl) return RunbookCoverageStatus.Missing;
            if (e.RunbookLastUpdatedAt.HasValue &&
                (now - e.RunbookLastUpdatedAt.Value).TotalDays <= freshnessMaxDays)
                return RunbookCoverageStatus.Covered;
            return RunbookCoverageStatus.Stale;
        }

        internal static ApiDocCoverageStatus ComputeApiDocCoverage(
            IDocumentationHealthReader.ServiceDocumentationEntry e)
        {
            if (e.ContractCount == 0) return ApiDocCoverageStatus.Absent;
            var total = e.ContractCount;
            var withDesc = e.ContractsWithDescription;
            var withEx = e.ContractsWithExamples;
            var withErr = e.ContractsWithErrorCodes;
            if (withDesc == total && withEx == total && withErr == total) return ApiDocCoverageStatus.Full;
            if (withDesc < total * 0.25m) return ApiDocCoverageStatus.Absent;
            return ApiDocCoverageStatus.Partial;
        }

        internal static DocFreshnessTier ComputeFreshnessTier(
            IDocumentationHealthReader.ServiceDocumentationEntry e,
            DateTimeOffset now,
            int freshnessMaxDays,
            HashSet<string> criticalTiers)
        {
            var docAge = e.DocLastUpdatedAt.HasValue
                ? (now - e.DocLastUpdatedAt.Value).TotalDays
                : double.MaxValue;

            if (docAge <= freshnessMaxDays * 0.5) return DocFreshnessTier.Fresh;
            if (docAge <= freshnessMaxDays) return DocFreshnessTier.Aging;
            if (criticalTiers.Contains(e.ServiceTier)) return DocFreshnessTier.Critical;
            return DocFreshnessTier.Stale;
        }

        internal static decimal ComputeDocHealthScore(
            RunbookCoverageStatus runbook,
            ApiDocCoverageStatus apiDoc,
            bool hasArchDoc,
            DocFreshnessTier freshness)
        {
            var runbookScore = runbook switch
            {
                RunbookCoverageStatus.Covered => 100m,
                RunbookCoverageStatus.Stale => 50m,
                RunbookCoverageStatus.Missing => 0m,
                _ => 0m
            };
            var apiScore = apiDoc switch
            {
                ApiDocCoverageStatus.Full => 100m,
                ApiDocCoverageStatus.Partial => 50m,
                ApiDocCoverageStatus.Absent => 0m,
                _ => 0m
            };
            var archScore = hasArchDoc ? 100m : 0m;
            var freshScore = freshness switch
            {
                DocFreshnessTier.Fresh => 100m,
                DocFreshnessTier.Aging => 70m,
                DocFreshnessTier.Stale => 30m,
                DocFreshnessTier.Critical => 0m,
                _ => 0m
            };

            return Math.Round(
                runbookScore * 0.35m +
                apiScore * 0.30m +
                archScore * 0.15m +
                freshScore * 0.20m, 1);
        }

        private static IReadOnlyList<TeamDocDebt> BuildStaleDocsByTeam(
            IReadOnlyList<ServiceDocHealthEntry> entries)
        {
            return entries
                .GroupBy(e => e.TeamName)
                .Select(g => new TeamDocDebt(
                    g.Key, g.Key,
                    g.Count(e => e.DocFreshnessTier is DocFreshnessTier.Stale or DocFreshnessTier.Critical),
                    g.Count(e => e.RunbookCoverage == RunbookCoverageStatus.Missing)))
                .Where(t => t.StaleDocCount > 0 || t.MissingRunbookCount > 0)
                .OrderByDescending(t => t.StaleDocCount + t.MissingRunbookCount)
                .ToList();
        }

        private static TenantDocumentationHealthSummary BuildSummary(
            IReadOnlyList<ServiceDocHealthEntry> serviceEntries,
            IReadOnlyList<IDocumentationHealthReader.ServiceDocumentationEntry> raw,
            IReadOnlyList<ServiceDocHealthEntry> criticalWithoutRunbook)
        {
            var withRunbook = serviceEntries.Count(e => e.RunbookCoverage == RunbookCoverageStatus.Covered);
            var withStaleRunbook = serviceEntries.Count(e => e.RunbookCoverage == RunbookCoverageStatus.Stale);
            var withoutRunbook = serviceEntries.Count(e => e.RunbookCoverage == RunbookCoverageStatus.Missing);

            var totalContracts = raw.Sum(e => e.ContractCount);
            var fullyDocContracts = raw.Sum(e =>
                e.ContractCount > 0 &&
                e.ContractsWithDescription == e.ContractCount &&
                e.ContractsWithExamples == e.ContractCount &&
                e.ContractsWithErrorCodes == e.ContractCount ? e.ContractCount : 0);
            var apiPct = totalContracts > 0
                ? Math.Round((decimal)fullyDocContracts / totalContracts * 100, 1)
                : 100m;

            var totalWeight = serviceEntries.Sum(e => TierWeight(e.ServiceTier));
            var weightedScore = totalWeight > 0
                ? serviceEntries.Sum(e => e.DocHealthScore * TierWeight(e.ServiceTier)) / totalWeight
                : 0m;
            var tenantScore = Math.Round(weightedScore, 1);

            var tier = criticalWithoutRunbook.Any()
                ? TenantDocHealthTier.Critical
                : tenantScore >= ExcellentThreshold
                    ? TenantDocHealthTier.Excellent
                    : tenantScore >= GoodThreshold
                        ? TenantDocHealthTier.Good
                        : tenantScore >= PartialThreshold
                            ? TenantDocHealthTier.Partial
                            : TenantDocHealthTier.Critical;

            return new TenantDocumentationHealthSummary(
                withRunbook, withStaleRunbook, withoutRunbook,
                apiPct, tier, tenantScore);
        }

        private static decimal TierWeight(string tier) => tier switch
        {
            "Critical" => 3m,
            "High" => 2m,
            _ => 1m
        };
    }
}
