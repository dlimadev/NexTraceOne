using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetSbomCoverageReport;

/// <summary>
/// Feature: GetSbomCoverageReport — análise de cobertura de SBOM por serviço.
///
/// Classifica cada serviço em <c>SbomCoverageTier</c>:
/// - <c>Covered</c>  — SBOM com idade ≤ FreshDays
/// - <c>Stale</c>    — SBOM com idade FreshDays+1 a StaleDays
/// - <c>Outdated</c> — SBOM com idade > StaleDays
/// - <c>Missing</c>  — sem SBOM registado
///
/// Produz:
/// - lista de serviços por tier
/// - TenantSbomHealthSummary com CoveredPct, TotalCriticalCves, TopVulnerableServices top 5
/// - LicenseRiskFlags — serviços com componentes GPL/AGPL
///
/// Wave AO.1 — Supply Chain &amp; Dependency Provenance (Catalog Contracts).
/// </summary>
public static class GetSbomCoverageReport
{
    internal const int DefaultFreshDays = 30;
    internal const int DefaultStaleDays = 90;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int FreshDays = DefaultFreshDays,
        int StaleDays = DefaultStaleDays) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.FreshDays).InclusiveBetween(1, 365);
            RuleFor(x => x.StaleDays).InclusiveBetween(1, 730);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum SbomCoverageTier { Covered, Stale, Outdated, Missing }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ServiceSbomRow(
        string ServiceId,
        string ServiceName,
        string TeamName,
        string ServiceTier,
        bool CustomerFacing,
        int ComponentCount,
        int HighSeverityCveCount,
        int CriticalCveCount,
        int OutdatedComponentCount,
        IReadOnlyDictionary<string, int> LicenseDistribution,
        int? SbomAge,
        SbomCoverageTier Tier,
        IReadOnlyList<string> GplOrAgplComponents);

    public sealed record LicenseRiskFlag(string ServiceId, string ServiceName, IReadOnlyList<string> RiskyComponents);

    public sealed record TenantSbomHealthSummary(
        decimal CoveredPct,
        int TotalCriticalCves,
        IReadOnlyList<ServiceSbomRow> TopVulnerableServices);

    public sealed record Report(
        string TenantId,
        int TotalServices,
        TenantSbomHealthSummary Summary,
        IReadOnlyList<ServiceSbomRow> ByService,
        IReadOnlyList<LicenseRiskFlag> LicenseRiskFlags,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        ISbomCoverageReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var entries = await reader.ListByTenantAsync(request.TenantId, cancellationToken);

            var rows = entries.Select(e =>
            {
                int? age = e.LastSbomRecordedAt.HasValue
                    ? (int)(now - e.LastSbomRecordedAt.Value).TotalDays
                    : null;

                var tier = age == null ? SbomCoverageTier.Missing
                    : age <= request.FreshDays ? SbomCoverageTier.Covered
                    : age <= request.StaleDays ? SbomCoverageTier.Stale
                    : SbomCoverageTier.Outdated;

                return new ServiceSbomRow(
                    e.ServiceId, e.ServiceName, e.TeamName, e.ServiceTier, e.CustomerFacing,
                    e.ComponentCount, e.HighSeverityCveCount, e.CriticalCveCount, e.OutdatedComponentCount,
                    (IReadOnlyDictionary<string, int>)e.LicenseDistribution,
                    age, tier, e.GplOrAgplComponents);
            }).ToList();

            var coveredCount = rows.Count(r => r.Tier == SbomCoverageTier.Covered);
            var coveredPct = rows.Count == 0 ? 0m : Math.Round((decimal)coveredCount / rows.Count * 100m, 2);
            var totalCritical = rows.Sum(r => r.CriticalCveCount);
            var topVulnerable = rows.Where(r => r.Tier != SbomCoverageTier.Missing)
                .OrderByDescending(r => r.CriticalCveCount).Take(5).ToList();

            var summary = new TenantSbomHealthSummary(coveredPct, totalCritical, topVulnerable);

            var licenseFlags = rows
                .Where(r => r.GplOrAgplComponents.Count > 0)
                .Select(r => new LicenseRiskFlag(r.ServiceId, r.ServiceName, r.GplOrAgplComponents))
                .ToList();

            return Result<Report>.Success(new Report(request.TenantId, rows.Count, summary, rows, licenseFlags, now));
        }
    }
}
