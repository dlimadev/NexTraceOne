using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetServiceApiGrowthReport;

/// <summary>
/// Feature: GetServiceApiGrowthReport — taxa de crescimento do número de contratos por serviço.
///
/// Compara o número de contratos distintos (ApiAssetIds activos em changelogs) por serviço
/// entre dois períodos:
/// - <b>período actual</b>: <c>now - LookbackDays</c> até <c>now</c>
/// - <b>período de comparação</b>: <c>now - LookbackDays - ComparisonPeriodDays</c> até <c>now - LookbackDays</c>
///
/// Classifica cada serviço por <c>GrowthTier</c>:
/// - <c>Stable</c>     — crescimento &lt; StableThresholdPct%  (ou sem variação)
/// - <c>Growing</c>    — crescimento entre StableThreshold% e RapidThreshold%
/// - <c>RapidGrowth</c>— crescimento entre RapidThreshold% e 100%
/// - <c>Exploding</c>  — crescimento &gt; 100% (risco de governance sprawl)
/// - <c>Shrinking</c>  — crescimento negativo (consolidação ou deprecation)
///
/// Sinaliza <c>GovernanceRisk</c> em serviços <c>RapidGrowth</c> ou <c>Exploding</c> que
/// também têm contratos com score de saúde abaixo de <c>GovernanceRiskHealthThreshold</c>.
///
/// Produz:
/// - distribuição global por GrowthTier no tenant
/// - top serviços com maior crescimento (percentagem)
/// - top serviços com GovernanceRisk activo
///
/// Orienta Architect e Platform Admin a prevenir API sprawl e garantir que o crescimento
/// do catálogo acompanha a governance.
///
/// Wave V.1 — Service API Growth Report (Catalog Contracts).
/// </summary>
public static class GetServiceApiGrowthReport
{
    private const int DefaultComparisonPeriodDays = 90;
    private const int DefaultLookbackDays = 30;
    private const int DefaultStableThresholdPct = 10;
    private const int DefaultRapidThresholdPct = 50;
    private const int DefaultGovernanceRiskHealthThreshold = 60;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela actual de análise, em dias (7–180, default 30).</para>
    /// <para><c>ComparisonPeriodDays</c>: janela histórica de comparação, em dias (30–365, default 90).</para>
    /// <para><c>StableThresholdPct</c>: crescimento máximo para GrowthTier Stable (1–49, default 10).</para>
    /// <para><c>RapidThresholdPct</c>: crescimento mínimo para GrowthTier RapidGrowth (StableThreshold+1, default 50).</para>
    /// <para><c>MaxTopServices</c>: número máximo de serviços no top crescimento (1–100, default 10).</para>
    /// <para><c>GovernanceRiskHealthThreshold</c>: score mínimo de saúde para não ter GovernanceRisk (1–100, default 60).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        int ComparisonPeriodDays = DefaultComparisonPeriodDays,
        int StableThresholdPct = DefaultStableThresholdPct,
        int RapidThresholdPct = DefaultRapidThresholdPct,
        int MaxTopServices = 10,
        int GovernanceRiskHealthThreshold = DefaultGovernanceRiskHealthThreshold) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Tier de crescimento de APIs de um serviço.</summary>
    public enum GrowthTier
    {
        /// <summary>Crescimento &lt; StableThreshold% — sem variação significativa.</summary>
        Stable,
        /// <summary>Crescimento entre StableThreshold% e RapidThreshold%.</summary>
        Growing,
        /// <summary>Crescimento entre RapidThreshold% e 100% — risco de sprawl.</summary>
        RapidGrowth,
        /// <summary>Crescimento &gt; 100% — acumulação descontrolada.</summary>
        Exploding,
        /// <summary>Crescimento negativo — consolidação ou deprecation.</summary>
        Shrinking
    }

    /// <summary>Distribuição de serviços por GrowthTier.</summary>
    public sealed record GrowthTierDistribution(
        int StableCount,
        int GrowingCount,
        int RapidGrowthCount,
        int ExplodingCount,
        int ShrinkingCount);

    /// <summary>Métricas de crescimento de APIs de um serviço.</summary>
    public sealed record ServiceApiGrowthEntry(
        string ServiceName,
        int CurrentContractCount,
        int PreviousContractCount,
        decimal GrowthRatePct,
        GrowthTier Tier,
        bool GovernanceRisk);

    /// <summary>Resultado do relatório de crescimento de APIs por serviço.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackDays,
        int ComparisonPeriodDays,
        int TotalServicesAnalyzed,
        GrowthTierDistribution TierDistribution,
        IReadOnlyList<ServiceApiGrowthEntry> TopGrowingServices,
        IReadOnlyList<ServiceApiGrowthEntry> TopGovernanceRiskServices,
        IReadOnlyList<ServiceApiGrowthEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 180);
            RuleFor(q => q.ComparisonPeriodDays).InclusiveBetween(30, 365);
            RuleFor(q => q.StableThresholdPct).InclusiveBetween(1, 49);
            RuleFor(q => q.RapidThresholdPct).GreaterThan(q => q.StableThresholdPct);
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 100);
            RuleFor(q => q.GovernanceRiskHealthThreshold).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IServiceAssetRepository _serviceRepo;
        private readonly IContractChangelogRepository _changelogRepo;
        private readonly IContractHealthScoreRepository _healthScoreRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IServiceAssetRepository serviceRepo,
            IContractChangelogRepository changelogRepo,
            IContractHealthScoreRepository healthScoreRepo,
            IDateTimeProvider clock)
        {
            _serviceRepo = Guard.Against.Null(serviceRepo);
            _changelogRepo = Guard.Against.Null(changelogRepo);
            _healthScoreRepo = Guard.Against.Null(healthScoreRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var currentFrom = now.AddDays(-query.LookbackDays);
            var compFrom = currentFrom.AddDays(-query.ComparisonPeriodDays);

            // 1. Get all active services to anchor the analysis
            var services = await _serviceRepo.ListAllAsync(cancellationToken);

            if (services.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    LookbackDays: query.LookbackDays,
                    ComparisonPeriodDays: query.ComparisonPeriodDays,
                    TotalServicesAnalyzed: 0,
                    TierDistribution: new GrowthTierDistribution(0, 0, 0, 0, 0),
                    TopGrowingServices: [],
                    TopGovernanceRiskServices: [],
                    AllServices: []));
            }

            // 2. Fetch changelogs for both periods
            var currentChangelogs = await _changelogRepo.ListByTenantInPeriodAsync(
                query.TenantId, currentFrom, now, cancellationToken);

            var compChangelogs = await _changelogRepo.ListByTenantInPeriodAsync(
                query.TenantId, compFrom, currentFrom, cancellationToken);

            // 3. Count distinct ApiAssetIds (active contracts) per service in each period
            var currentByService = currentChangelogs
                .GroupBy(c => c.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(c => c.ApiAssetId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    StringComparer.OrdinalIgnoreCase);

            var compByService = compChangelogs
                .GroupBy(c => c.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(c => c.ApiAssetId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    StringComparer.OrdinalIgnoreCase);

            // 4. Get low-health contracts for GovernanceRisk detection
            var lowHealthScores = await _healthScoreRepo.ListBelowThresholdAsync(
                query.GovernanceRiskHealthThreshold, cancellationToken);

            var lowHealthApiAssetIds = new HashSet<string>(
                lowHealthScores.Select(s => s.ApiAssetId.ToString()),
                StringComparer.OrdinalIgnoreCase);

            // Map low-health ApiAssets → service names via current period changelogs
            var servicesWithLowHealthContracts = currentChangelogs
                .Where(c => lowHealthApiAssetIds.Contains(c.ApiAssetId))
                .Select(c => c.ServiceName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 5. Build entries only for services active in at least one period
            var activeSvcNames = services
                .Select(s => s.Name)
                .Where(n =>
                    currentByService.ContainsKey(n) || compByService.ContainsKey(n))
                .ToList();

            var entries = new List<ServiceApiGrowthEntry>(activeSvcNames.Count);

            foreach (var svcName in activeSvcNames)
            {
                currentByService.TryGetValue(svcName, out var current);
                compByService.TryGetValue(svcName, out var previous);

                decimal growthRate = ComputeGrowthRate(current, previous);
                var tier = ClassifyTier(growthRate, query.StableThresholdPct, query.RapidThresholdPct);

                bool governanceRisk = tier is GrowthTier.RapidGrowth or GrowthTier.Exploding
                    && servicesWithLowHealthContracts.Contains(svcName);

                entries.Add(new ServiceApiGrowthEntry(
                    ServiceName: svcName,
                    CurrentContractCount: current,
                    PreviousContractCount: previous,
                    GrowthRatePct: growthRate,
                    Tier: tier,
                    GovernanceRisk: governanceRisk));
            }

            if (entries.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    LookbackDays: query.LookbackDays,
                    ComparisonPeriodDays: query.ComparisonPeriodDays,
                    TotalServicesAnalyzed: 0,
                    TierDistribution: new GrowthTierDistribution(0, 0, 0, 0, 0),
                    TopGrowingServices: [],
                    TopGovernanceRiskServices: [],
                    AllServices: []));
            }

            int stableCount = entries.Count(e => e.Tier == GrowthTier.Stable);
            int growingCount = entries.Count(e => e.Tier == GrowthTier.Growing);
            int rapidCount = entries.Count(e => e.Tier == GrowthTier.RapidGrowth);
            int explodingCount = entries.Count(e => e.Tier == GrowthTier.Exploding);
            int shrinkingCount = entries.Count(e => e.Tier == GrowthTier.Shrinking);

            var topGrowing = entries
                .OrderByDescending(e => e.GrowthRatePct)
                .Take(query.MaxTopServices)
                .ToList();

            var topGovernanceRisk = entries
                .Where(e => e.GovernanceRisk)
                .OrderByDescending(e => e.GrowthRatePct)
                .Take(query.MaxTopServices)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackDays: query.LookbackDays,
                ComparisonPeriodDays: query.ComparisonPeriodDays,
                TotalServicesAnalyzed: entries.Count,
                TierDistribution: new GrowthTierDistribution(stableCount, growingCount, rapidCount, explodingCount, shrinkingCount),
                TopGrowingServices: topGrowing,
                TopGovernanceRiskServices: topGovernanceRisk,
                AllServices: entries.OrderBy(e => e.ServiceName).ToList()));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Calcula a taxa de crescimento de contratos entre períodos.
        /// Quando o período anterior tem 0 contratos, retorna 100% (serviço novo com actividade).
        /// </summary>
        private static decimal ComputeGrowthRate(int current, int previous)
        {
            if (previous == 0)
                return current > 0 ? 100m : 0m;
            return Math.Round((decimal)(current - previous) / previous * 100m, 1);
        }

        private static GrowthTier ClassifyTier(decimal growthPct, int stableThreshold, int rapidThreshold) =>
            growthPct < 0m ? GrowthTier.Shrinking
            : growthPct < stableThreshold ? GrowthTier.Stable
            : growthPct < rapidThreshold ? GrowthTier.Growing
            : growthPct <= 100m ? GrowthTier.RapidGrowth
            : GrowthTier.Exploding;
    }
}
