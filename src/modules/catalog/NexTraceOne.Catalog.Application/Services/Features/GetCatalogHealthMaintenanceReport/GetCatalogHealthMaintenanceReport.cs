using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services.Features.GetCatalogHealthMaintenanceReport;

/// <summary>
/// Feature: GetCatalogHealthMaintenanceReport — qualidade de manutenção do catálogo de serviços.
///
/// Calcula <c>CatalogQualityScore</c> (0–100) por 5 dimensões ponderadas:
/// - DescriptionCompleteness (20%) — ≥ MinDescriptionWords palavras
/// - OwnershipFreshness       (25%) — actualizado nos últimos OwnershipStaleDays dias
/// - ContractCoverage         (25%) — ≥ 1 contrato Approved registado
/// - DependencyMapFreshness   (15%) — actualizado nos últimos DependencyStaleDays dias
/// - RunbookLinked            (15%) — runbook activo com step de resolução
///
/// <c>CatalogQualityTier</c>: Excellent ≥85 / Good ≥65 / Fair ≥40 / Poor &lt;40
/// <c>TenantCatalogHealthScore</c> = média ponderada por tier (Critical 3×, Standard 2×, Internal 1×)
///
/// Wave AM.3 — Auto-Cataloging &amp; Service Discovery Intelligence (Catalog Services).
/// </summary>
public static class GetCatalogHealthMaintenanceReport
{
    // ── Score dimension weights ────────────────────────────────────────────
    private const decimal DescriptionWeight = 0.20m;
    private const decimal OwnershipWeight = 0.25m;
    private const decimal ContractWeight = 0.25m;
    private const decimal DependencyWeight = 0.15m;
    private const decimal RunbookWeight = 0.15m;

    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal ExcellentThreshold = 85m;
    private const decimal GoodThreshold = 65m;
    private const decimal FairThreshold = 40m;

    // ── Service tier weights ───────────────────────────────────────────────
    private const decimal CriticalServiceWeight = 3m;
    private const decimal StandardServiceWeight = 2m;
    private const decimal InternalServiceWeight = 1m;

    internal const int DefaultMinDescriptionWords = 10;
    internal const int DefaultOwnershipStaleDays = 90;
    internal const int DefaultDependencyStaleDays = 60;
    internal const int DefaultMaintenanceStaleDays = 180;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int MinDescriptionWords = DefaultMinDescriptionWords,
        int OwnershipStaleDays = DefaultOwnershipStaleDays,
        int DependencyStaleDays = DefaultDependencyStaleDays,
        int MaintenanceStaleDays = DefaultMaintenanceStaleDays) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.MinDescriptionWords).InclusiveBetween(1, 100);
            RuleFor(x => x.OwnershipStaleDays).InclusiveBetween(7, 365);
            RuleFor(x => x.DependencyStaleDays).InclusiveBetween(7, 365);
            RuleFor(x => x.MaintenanceStaleDays).InclusiveBetween(7, 730);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum CatalogQualityTier { Excellent, Good, Fair, Poor }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ServiceQualityScores(
        decimal DescriptionScore,
        decimal OwnershipScore,
        decimal ContractScore,
        decimal DependencyScore,
        decimal RunbookScore);

    public sealed record ServiceQualityRow(
        string ServiceId,
        string ServiceName,
        string ServiceTier,
        decimal CatalogQualityScore,
        CatalogQualityTier QualityTier,
        ServiceQualityScores Scores,
        IReadOnlyList<string> Issues,
        bool IsStaleEntry);

    public sealed record CampaignItem(
        string ServiceId,
        string ServiceName,
        string ServiceTier,
        CatalogQualityTier QualityTier,
        IReadOnlyList<string> Issues);

    public sealed record Report(
        string TenantId,
        int TotalServicesAnalyzed,
        int ExcellentCount,
        int GoodCount,
        int FairCount,
        int PoorCount,
        decimal TenantCatalogHealthScore,
        CatalogQualityTier OverallTier,
        IReadOnlyList<ServiceQualityRow> ByService,
        IReadOnlyList<CampaignItem> CampaignList,
        IReadOnlyList<ServiceQualityRow> StaleEntryList,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        ICatalogHealthMaintenanceReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var entries = await reader.ListByTenantAsync(request.TenantId, cancellationToken);

            var rows = entries.Select(e =>
            {
                var issues = new List<string>();

                // Description completeness
                var descScore = e.DescriptionWordCount >= request.MinDescriptionWords ? 100m : 0m;
                if (descScore < 100m) issues.Add("Description too short or missing");

                // Ownership freshness
                var ownershipScore = e.LastOwnershipUpdate.HasValue
                    && (now - e.LastOwnershipUpdate.Value).TotalDays <= request.OwnershipStaleDays
                    ? 100m : 0m;
                if (ownershipScore < 100m) issues.Add("Ownership record stale or missing");

                // Contract coverage
                var contractScore = e.HasApprovedContract ? 100m : 0m;
                if (contractScore < 100m) issues.Add("No approved contract registered");

                // Dependency map freshness
                var depScore = e.LastDependencyUpdate.HasValue
                    && (now - e.LastDependencyUpdate.Value).TotalDays <= request.DependencyStaleDays
                    ? 100m : 0m;
                if (depScore < 100m) issues.Add("Dependency map stale or missing");

                // Runbook linked
                var runbookScore = e.HasActiveRunbook ? 100m : 0m;
                if (runbookScore < 100m) issues.Add("No active runbook linked");

                var compositeScore = Math.Round(
                    descScore * DescriptionWeight +
                    ownershipScore * OwnershipWeight +
                    contractScore * ContractWeight +
                    depScore * DependencyWeight +
                    runbookScore * RunbookWeight, 2);

                var tier = compositeScore >= ExcellentThreshold ? CatalogQualityTier.Excellent
                    : compositeScore >= GoodThreshold ? CatalogQualityTier.Good
                    : compositeScore >= FairThreshold ? CatalogQualityTier.Fair
                    : CatalogQualityTier.Poor;

                var isStale = e.LastMaintenanceActivity.HasValue
                    ? (now - e.LastMaintenanceActivity.Value).TotalDays > request.MaintenanceStaleDays
                    : true;

                return new ServiceQualityRow(
                    e.ServiceId, e.ServiceName, e.ServiceTier,
                    compositeScore, tier,
                    new ServiceQualityScores(descScore, ownershipScore, contractScore, depScore, runbookScore),
                    issues,
                    isStale);
            }).ToList();

            // Tenant health score weighted by service tier
            var weightedSum = rows.Sum(r =>
            {
                var weight = r.ServiceTier.Equals("Critical", StringComparison.OrdinalIgnoreCase) ? CriticalServiceWeight
                    : r.ServiceTier.Equals("Standard", StringComparison.OrdinalIgnoreCase) ? StandardServiceWeight
                    : InternalServiceWeight;
                return r.CatalogQualityScore * weight;
            });
            var totalWeight = rows.Sum(r =>
                r.ServiceTier.Equals("Critical", StringComparison.OrdinalIgnoreCase) ? CriticalServiceWeight
                : r.ServiceTier.Equals("Standard", StringComparison.OrdinalIgnoreCase) ? StandardServiceWeight
                : InternalServiceWeight);

            var tenantScore = rows.Count == 0 ? 100m : Math.Round(weightedSum / totalWeight, 2);

            var overallTier = tenantScore >= ExcellentThreshold ? CatalogQualityTier.Excellent
                : tenantScore >= GoodThreshold ? CatalogQualityTier.Good
                : tenantScore >= FairThreshold ? CatalogQualityTier.Fair
                : CatalogQualityTier.Poor;

            // Campaign list: Poor and Fair sorted by Critical first
            var campaign = rows
                .Where(r => r.QualityTier is CatalogQualityTier.Poor or CatalogQualityTier.Fair)
                .OrderBy(r => r.ServiceTier.Equals("Critical", StringComparison.OrdinalIgnoreCase) ? 0
                    : r.ServiceTier.Equals("Standard", StringComparison.OrdinalIgnoreCase) ? 1 : 2)
                .ThenBy(r => r.CatalogQualityScore)
                .Select(r => new CampaignItem(r.ServiceId, r.ServiceName, r.ServiceTier, r.QualityTier, r.Issues))
                .ToList();

            var staleList = rows.Where(r => r.IsStaleEntry).OrderBy(r => r.CatalogQualityScore).ToList();

            return Result<Report>.Success(new Report(
                request.TenantId,
                rows.Count,
                rows.Count(r => r.QualityTier == CatalogQualityTier.Excellent),
                rows.Count(r => r.QualityTier == CatalogQualityTier.Good),
                rows.Count(r => r.QualityTier == CatalogQualityTier.Fair),
                rows.Count(r => r.QualityTier == CatalogQualityTier.Poor),
                tenantScore, overallTier, rows, campaign, staleList, now));
        }
    }
}
