using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetDependencyUpdateFreshnessReport;

/// <summary>
/// Feature: GetDependencyUpdateFreshnessReport — análise de "frescor" das dependências por serviço.
///
/// Para cada serviço activo no catálogo, calcula quantos dias passaram desde a última
/// alteração de contrato registada (<c>ContractChangelog</c>) associada a esse serviço.
/// Cruza com <c>VulnerabilityAdvisoryRecord</c> para identificar serviços desactualizados
/// com vulnerabilidades abertas (<c>VulnerabilityGap</c> flag).
///
/// Classifica cada serviço por <c>FreshnessTier</c>:
/// - <c>Fresh</c>    — últimos 30 dias (default, configurável)
/// - <c>Aging</c>    — 31–90 dias
/// - <c>Stale</c>    — 91–180 dias
/// - <c>Critical</c> — mais de 180 dias sem actualização registada ou sem changelog
///
/// Produz:
/// - distribuição global por FreshnessTier no tenant
/// - top serviços mais desactualizados com número de vulns abertas associadas
/// - lista completa de serviços analisados
///
/// Orienta Architect, Tech Lead e Security a priorizar upgrades de dependências por risco real.
///
/// Wave U.2 — Dependency Update Freshness Report (Catalog Contracts).
/// </summary>
public static class GetDependencyUpdateFreshnessReport
{
    // ── FreshnessTier thresholds (dias) ────────────────────────────────────
    private const int DefaultFreshThresholdDays = 30;
    private const int DefaultAgingThresholdDays = 90;
    private const int DefaultStaleThresholdDays = 180;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela de análise para changelogs (30–365, default 180).</para>
    /// <para><c>TopStaleCount</c>: número máximo de serviços mais desactualizados a listar (1–100, default 10).</para>
    /// <para><c>FreshThresholdDays</c>: dias máximos para tier Fresh (1–30, default 30).</para>
    /// <para><c>AgingThresholdDays</c>: dias máximos para tier Aging (31–90, default 90).</para>
    /// <para><c>StaleThresholdDays</c>: dias máximos para tier Stale (91–365, default 180).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 180,
        int TopStaleCount = 10,
        int FreshThresholdDays = DefaultFreshThresholdDays,
        int AgingThresholdDays = DefaultAgingThresholdDays,
        int StaleThresholdDays = DefaultStaleThresholdDays) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Tier de frescor de dependências de um serviço.</summary>
    public enum FreshnessTier
    {
        /// <summary>Última alteração nos últimos FreshThresholdDays dias.</summary>
        Fresh,
        /// <summary>Última alteração entre FreshThreshold e AgingThreshold dias.</summary>
        Aging,
        /// <summary>Última alteração entre AgingThreshold e StaleThreshold dias.</summary>
        Stale,
        /// <summary>Última alteração há mais de StaleThreshold dias (ou sem changelog).</summary>
        Critical
    }

    /// <summary>Distribuição de serviços por FreshnessTier.</summary>
    public sealed record FreshnessTierDistribution(
        int FreshCount,
        int AgingCount,
        int StaleCount,
        int CriticalCount);

    /// <summary>Entrada de análise de frescor de dependências para um serviço.</summary>
    public sealed record ServiceFreshnessEntry(
        string ServiceName,
        int DaysSinceLastDependencyChange,
        FreshnessTier Tier,
        int OpenVulnerabilityCount,
        bool VulnerabilityGap);

    /// <summary>Resultado do relatório de frescor de dependências.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int TotalServicesAnalyzed,
        FreshnessTierDistribution TierDistribution,
        IReadOnlyList<ServiceFreshnessEntry> TopStaleServices,
        IReadOnlyList<ServiceFreshnessEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(30, 365);
            RuleFor(q => q.TopStaleCount).InclusiveBetween(1, 100);
            RuleFor(q => q.FreshThresholdDays).InclusiveBetween(1, 30);
            RuleFor(q => q.AgingThresholdDays).GreaterThan(q => q.FreshThresholdDays);
            RuleFor(q => q.StaleThresholdDays).GreaterThan(q => q.AgingThresholdDays);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IServiceAssetRepository _serviceRepo;
        private readonly IContractChangelogRepository _changelogRepo;
        private readonly IVulnerabilityAdvisoryRepository _vulnRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IServiceAssetRepository serviceRepo,
            IContractChangelogRepository changelogRepo,
            IVulnerabilityAdvisoryRepository vulnRepo,
            IDateTimeProvider clock)
        {
            _serviceRepo = Guard.Against.Null(serviceRepo);
            _changelogRepo = Guard.Against.Null(changelogRepo);
            _vulnRepo = Guard.Against.Null(vulnRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);

            // 1. Fetch all services
            var services = await _serviceRepo.ListAllAsync(cancellationToken);

            if (services.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    LookbackDays: query.LookbackDays,
                    TotalServicesAnalyzed: 0,
                    TierDistribution: new FreshnessTierDistribution(0, 0, 0, 0),
                    TopStaleServices: [],
                    AllServices: []));
            }

            // 2. Fetch changelogs in the lookback period for the tenant
            var changelogs = await _changelogRepo.ListByTenantInPeriodAsync(
                query.TenantId, from, now, cancellationToken);

            // Build lookup: serviceName → most recent changelog date
            var latestChangeByService = changelogs
                .GroupBy(c => c.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(c => c.CreatedAt),
                    StringComparer.OrdinalIgnoreCase);

            // 3. Build per-service entries
            var entries = new List<ServiceFreshnessEntry>();

            foreach (var svc in services)
            {
                int daysSince;
                if (latestChangeByService.TryGetValue(svc.Name, out var lastChange))
                {
                    daysSince = (int)(now - lastChange).TotalDays;
                }
                else
                {
                    // No changelog in period → treat as Critical (beyond StaleThreshold)
                    daysSince = query.StaleThresholdDays + 1;
                }

                var tier = ClassifyTier(daysSince, query.FreshThresholdDays, query.AgingThresholdDays, query.StaleThresholdDays);

                // 4. Vulnerability count for Stale/Critical services
                int openVulnCount = 0;
                bool vulnGap = false;

                if (tier is FreshnessTier.Stale or FreshnessTier.Critical)
                {
                    openVulnCount = await _vulnRepo.CountByServiceAndSeverityAsync(
                        svc.Id.Value, VulnerabilitySeverity.Low, activeOnly: true, cancellationToken);
                    vulnGap = openVulnCount > 0;
                }

                entries.Add(new ServiceFreshnessEntry(
                    ServiceName: svc.Name,
                    DaysSinceLastDependencyChange: daysSince,
                    Tier: tier,
                    OpenVulnerabilityCount: openVulnCount,
                    VulnerabilityGap: vulnGap));
            }

            int freshCount = entries.Count(e => e.Tier == FreshnessTier.Fresh);
            int agingCount = entries.Count(e => e.Tier == FreshnessTier.Aging);
            int staleCount = entries.Count(e => e.Tier == FreshnessTier.Stale);
            int criticalCount = entries.Count(e => e.Tier == FreshnessTier.Critical);

            var topStale = entries
                .OrderByDescending(e => e.DaysSinceLastDependencyChange)
                .ThenByDescending(e => e.OpenVulnerabilityCount)
                .Take(query.TopStaleCount)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                TotalServicesAnalyzed: entries.Count,
                TierDistribution: new FreshnessTierDistribution(freshCount, agingCount, staleCount, criticalCount),
                TopStaleServices: topStale,
                AllServices: entries.OrderBy(e => e.ServiceName).ToList()));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static FreshnessTier ClassifyTier(int daysSince, int fresh, int aging, int stale) =>
            daysSince <= fresh ? FreshnessTier.Fresh
            : daysSince <= aging ? FreshnessTier.Aging
            : daysSince <= stale ? FreshnessTier.Stale
            : FreshnessTier.Critical;
    }
}
