using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetSupplyChainRiskReport;

/// <summary>
/// Feature: GetSupplyChainRiskReport — risco consolidado da cadeia de fornecimento.
///
/// Calcula <c>ComponentRiskScore</c> (0–100) por componente vulnerável:
/// - CveSeverity (50%)        — severidade: Critical=100 / High=75 / Medium=50 / Low=25
/// - ExposedServices (30%)    — % de serviços do tenant afectados
/// - CustomerFacingExposed (20%) — flag binário (100 se expostos)
///
/// <c>SupplyChainRiskTier</c>: Secure ≤20 / Monitored ≤50 / Exposed ≤75 / Critical >75
///
/// Produz PrioritizedPatchList ordenada por ComponentRiskScore DESC para guiar esforço de patching.
///
/// Wave AO.3 — Supply Chain &amp; Dependency Provenance (Catalog Contracts).
/// </summary>
public static class GetSupplyChainRiskReport
{
    private const decimal CveSeverityWeight = 0.50m;
    private const decimal ExposureWeight = 0.30m;
    private const decimal CustomerFacingWeight = 0.20m;

    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal CriticalThreshold = 75m;
    private const decimal ExposedThreshold = 50m;
    private const decimal MonitoredThreshold = 20m;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(string TenantId) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum SupplyChainRiskTier { Secure, Monitored, Exposed, Critical }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ComponentRiskRow(
        string ComponentName,
        string ComponentVersion,
        string HighestCveSeverity,
        int CveCount,
        int AffectedServicesCount,
        int TransitiveServicesCount,
        int TotalExposedServices,
        decimal ExposureBlastRadiusPct,
        bool HasCustomerFacingExposed,
        decimal ComponentRiskScore,
        SupplyChainRiskTier Tier,
        int? UnpatchedWindowDays,
        string? FixVersion);

    public sealed record PatchPriorityItem(
        string ComponentName,
        string ComponentVersion,
        string? FixVersion,
        decimal ComponentRiskScore,
        int AffectedServicesCount);

    public sealed record Report(
        string TenantId,
        int TotalVulnerableComponents,
        decimal TenantSupplyChainRiskScore,
        SupplyChainRiskTier TenantRiskTier,
        IReadOnlyList<ComponentRiskRow> ByComponent,
        IReadOnlyList<PatchPriorityItem> PrioritizedPatchList,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        ISupplyChainRiskReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var entries = await reader.ListVulnerableComponentsByTenantAsync(request.TenantId, cancellationToken);

            var rows = entries.Select(e =>
            {
                var totalExposed = e.DirectlyAffectedServiceIds
                    .Union(e.TransitivelyAffectedServiceIds).Count();

                var exposurePct = e.TotalServicesInTenant == 0 ? 0m
                    : Math.Round((decimal)totalExposed / e.TotalServicesInTenant * 100m, 2);

                var severityScore = e.HighestCveSeverity?.ToUpperInvariant() switch
                {
                    "CRITICAL" => 100m,
                    "HIGH" => 75m,
                    "MEDIUM" => 50m,
                    "LOW" => 25m,
                    _ => 10m
                };
                var exposureScore = Math.Min(exposurePct, 100m);
                var cfScore = e.HasCustomerFacingExposed ? 100m : 0m;

                var riskScore = Math.Round(
                    severityScore * CveSeverityWeight +
                    exposureScore * ExposureWeight +
                    cfScore * CustomerFacingWeight, 2);

                var tier = riskScore > CriticalThreshold ? SupplyChainRiskTier.Critical
                    : riskScore > ExposedThreshold ? SupplyChainRiskTier.Exposed
                    : riskScore > MonitoredThreshold ? SupplyChainRiskTier.Monitored
                    : SupplyChainRiskTier.Secure;

                int? unpatchedDays = e.CvePublishedAt.HasValue
                    ? (int)(now - e.CvePublishedAt.Value).TotalDays
                    : null;

                return new ComponentRiskRow(
                    e.ComponentName, e.ComponentVersion, e.HighestCveSeverity ?? "Unknown",
                    e.CveCount, e.DirectlyAffectedServiceIds.Count,
                    e.TransitivelyAffectedServiceIds.Count, totalExposed,
                    exposurePct, e.HasCustomerFacingExposed,
                    riskScore, tier, unpatchedDays, e.FixVersion);
            }).ToList();

            var tenantScore = rows.Count == 0 ? 0m : Math.Round(rows.Average(r => r.ComponentRiskScore), 2);
            var tenantTier = tenantScore > CriticalThreshold ? SupplyChainRiskTier.Critical
                : tenantScore > ExposedThreshold ? SupplyChainRiskTier.Exposed
                : tenantScore > MonitoredThreshold ? SupplyChainRiskTier.Monitored
                : SupplyChainRiskTier.Secure;

            var patchList = rows.OrderByDescending(r => r.ComponentRiskScore)
                .Select(r => new PatchPriorityItem(r.ComponentName, r.ComponentVersion, r.FixVersion,
                    r.ComponentRiskScore, r.AffectedServicesCount))
                .ToList();

            return Result<Report>.Success(new Report(
                request.TenantId, rows.Count, tenantScore, tenantTier, rows, patchList, now));
        }
    }
}
