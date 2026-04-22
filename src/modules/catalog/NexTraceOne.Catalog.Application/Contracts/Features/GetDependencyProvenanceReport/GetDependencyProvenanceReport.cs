using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetDependencyProvenanceReport;

/// <summary>
/// Feature: GetDependencyProvenanceReport — proveniência de dependências por componente.
///
/// Classifica cada componente em <c>ProvenanceTier</c>:
/// - <c>Trusted</c>  — approved registry + LicenseRisk Safe + 0 Critical CVEs
/// - <c>Review</c>   — approved registry mas LicenseRisk Attention OU HighSeverity CVEs
/// - <c>HighRisk</c> — GPL/AGPL licença OU unapproved registry com CVEs
/// - <c>Blocked</c>  — Critical CVEs + CustomerFacing OU unapproved registry + HighRisk license
///
/// Produz TenantProvenanceSummary com TrustedPct, UnapprovedRegistryComponents,
/// HighRiskLicenseComponents, MostUsedComponents top-20, SinglePointOfFailureComponents.
///
/// Wave AO.2 — Supply Chain &amp; Dependency Provenance (Catalog Contracts).
/// </summary>
public static class GetDependencyProvenanceReport
{
    internal static readonly string[] DefaultApprovedRegistries = ["nuget.org", "npmjs.com"];
    internal static readonly string[] DefaultHighRiskLicenses = ["GPL", "AGPL"];
    internal const int DefaultSpofServiceThreshold = 5;
    private const int TopComponentsCount = 20;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        IReadOnlyList<string>? ApprovedRegistries = null,
        IReadOnlyList<string>? HighRiskLicenses = null,
        int SpofServiceThreshold = DefaultSpofServiceThreshold) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SpofServiceThreshold).InclusiveBetween(2, 100);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum LicenseRisk { Safe, Attention, HighRisk, Unknown }
    public enum ProvenanceTier { Trusted, Review, HighRisk, Blocked }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ComponentProvenanceRow(
        string ComponentName,
        IReadOnlyList<string> VersionsInUse,
        int ServiceCount,
        string RegistryOrigin,
        bool IsApprovedRegistry,
        string LicenseType,
        LicenseRisk LicenseRisk,
        int TotalCveCount,
        string HighestSeverity,
        ProvenanceTier Tier);

    public sealed record TenantProvenanceSummary(
        decimal TrustedPct,
        decimal HighRiskPct,
        decimal BlockedPct,
        IReadOnlyList<ComponentProvenanceRow> UnapprovedRegistryComponents,
        IReadOnlyList<ComponentProvenanceRow> HighRiskLicenseComponents,
        IReadOnlyList<ComponentProvenanceRow> CriticalVulnerabilityComponents);

    public sealed record Report(
        string TenantId,
        int TotalComponents,
        TenantProvenanceSummary Summary,
        IReadOnlyList<ComponentProvenanceRow> ByComponent,
        IReadOnlyList<ComponentProvenanceRow> MostUsedComponents,
        IReadOnlyList<ComponentProvenanceRow> SinglePointOfFailureComponents,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IDependencyProvenanceReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var approvedRegistries = request.ApprovedRegistries ?? DefaultApprovedRegistries;
            var highRiskLicenses = request.HighRiskLicenses ?? DefaultHighRiskLicenses;

            var entries = await reader.ListComponentsByTenantAsync(
                request.TenantId, approvedRegistries, highRiskLicenses,
                request.SpofServiceThreshold, cancellationToken);

            var rows = entries.Select(e =>
            {
                var licenseRisk = ClassifyLicense(e.LicenseType, highRiskLicenses);
                var tier = ClassifyTier(e.IsApprovedRegistry, licenseRisk, e.HighestSeverity);

                return new ComponentProvenanceRow(
                    e.ComponentName, e.VersionsInUse, e.ServiceCount,
                    e.RegistryOrigin, e.IsApprovedRegistry,
                    e.LicenseType, licenseRisk, e.TotalCveCount, e.HighestSeverity, tier);
            }).ToList();

            var trusted = rows.Count(r => r.Tier == ProvenanceTier.Trusted);
            var highRisk = rows.Count(r => r.Tier == ProvenanceTier.HighRisk);
            var blocked = rows.Count(r => r.Tier == ProvenanceTier.Blocked);
            var total = Math.Max(1, rows.Count);

            var summary = new TenantProvenanceSummary(
                Math.Round((decimal)trusted / total * 100m, 2),
                Math.Round((decimal)highRisk / total * 100m, 2),
                Math.Round((decimal)blocked / total * 100m, 2),
                rows.Where(r => !r.IsApprovedRegistry).ToList(),
                rows.Where(r => r.LicenseRisk == LicenseRisk.HighRisk).ToList(),
                rows.Where(r => r.HighestSeverity == "Critical" && r.TotalCveCount > 0).ToList());

            var mostUsed = rows.OrderByDescending(r => r.ServiceCount).Take(TopComponentsCount).ToList();
            var spof = rows.Where(r => r.ServiceCount >= request.SpofServiceThreshold).ToList();

            return Result<Report>.Success(new Report(
                request.TenantId, rows.Count, summary, rows, mostUsed, spof, now));
        }

        private static LicenseRisk ClassifyLicense(string license, IReadOnlyList<string> highRiskLicenses)
        {
            if (string.IsNullOrWhiteSpace(license)) return LicenseRisk.Unknown;
            var upper = license.ToUpperInvariant();
            // Check LGPL before GPL/AGPL to avoid false-positive (LGPL contains "GPL")
            if (upper.Contains("LGPL")) return LicenseRisk.Attention;
            if (highRiskLicenses.Any(r => upper.Contains(r.ToUpperInvariant()))) return LicenseRisk.HighRisk;
            if (upper.Contains("MIT") || upper.Contains("APACHE") || upper.Contains("BSD") ||
                upper.Contains("ISC") || upper.Contains("UNLICENSE")) return LicenseRisk.Safe;
            return LicenseRisk.Unknown;
        }

        private static ProvenanceTier ClassifyTier(bool isApproved, LicenseRisk licenseRisk, string highestSeverity)
        {
            var hasCritical = highestSeverity?.Equals("Critical", StringComparison.OrdinalIgnoreCase) ?? false;
            var hasHigh = highestSeverity?.Equals("High", StringComparison.OrdinalIgnoreCase) ?? false;

            if (!isApproved && licenseRisk == LicenseRisk.HighRisk) return ProvenanceTier.Blocked;
            if (hasCritical) return ProvenanceTier.Blocked;
            if (!isApproved || licenseRisk == LicenseRisk.HighRisk) return ProvenanceTier.HighRisk;
            if (licenseRisk == LicenseRisk.Attention || hasHigh) return ProvenanceTier.Review;
            if (licenseRisk == LicenseRisk.Unknown) return ProvenanceTier.Review;
            return ProvenanceTier.Trusted;
        }
    }
}
