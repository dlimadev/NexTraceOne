using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseFrequencyDeviationReport;

/// <summary>
/// Feature: GetReleaseFrequencyDeviationReport — deteção de desvios bruscos de frequência de
/// deployment por serviço entre um período recente e um período histórico de referência.
///
/// Compara <c>DeploysPerDay</c> em dois períodos:
/// - <b>recente</b>:    últimos <c>RecentDays</c> dias
/// - <b>histórico</b>:  janela de <c>HistoricalDays</c> dias imediatamente anterior ao recente
///
/// Classifica cada serviço por <c>FrequencyDeviation</c>:
/// - <c>Accelerating</c>  — desvio &gt; +50% (rush de deployments — risco de qualidade)
/// - <c>Stable</c>        — desvio entre -50% e +50%
/// - <c>Decelerating</c>  — desvio &lt; -50% (possível bloqueio ou loss of momentum)
/// - <c>Stalled</c>       — zero releases recentes mas com histórico (potencial paralisia)
/// - <c>New</c>           — sem histórico, apenas releases recentes (serviço novo)
///
/// Sinaliza <c>RiskFlag</c>:
/// - <c>Accelerating</c> com <c>RecentSuccessRatePct &lt; 80%</c> — rush sem qualidade
/// - <c>Stalled</c> — paralisia operacional independente de tier
///
/// Produz:
/// - distribuição global por FrequencyDeviation no tenant
/// - top serviços com maior desvio positivo e negativo
///
/// Complementa o GetDeploymentCadenceReport (Wave K) com perspetiva de variação de ritmo.
///
/// Wave V.3 — Release Frequency Deviation Report (ChangeGovernance ChangeIntelligence).
/// </summary>
public static class GetReleaseFrequencyDeviationReport
{
    private const decimal AcceleratingThreshold = 50m;   // > +50%
    private const decimal DeceleratingThreshold = -50m;  // < -50%
    private const decimal RiskSuccessRateThreshold = 80m; // < 80% success rate

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>RecentDays</c>: janela recente de comparação em dias (7–60, default 30).</para>
    /// <para><c>HistoricalDays</c>: janela histórica de baseline em dias (30–365, default 90).</para>
    /// <para><c>MaxTopServices</c>: número máximo de serviços no ranking de desvio (1–200, default 20).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente (null = todos).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int RecentDays = 30,
        int HistoricalDays = 90,
        int MaxTopServices = 20,
        string? Environment = null) : IQuery<Report>;

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Classificação de desvio de frequência de deployments de um serviço.</summary>
    public enum FrequencyDeviation
    {
        /// <summary>Desvio &gt; +50% — rush de deployments, risco de qualidade.</summary>
        Accelerating,
        /// <summary>Desvio entre -50% e +50% — ritmo estável.</summary>
        Stable,
        /// <summary>Desvio &lt; -50% — possível bloqueio ou loss of momentum.</summary>
        Decelerating,
        /// <summary>Zero releases recentes mas com histórico — potencial paralisia operacional.</summary>
        Stalled,
        /// <summary>Sem histórico, apenas releases recentes — serviço novo.</summary>
        New
    }

    /// <summary>Distribuição global de serviços por FrequencyDeviation.</summary>
    public sealed record FrequencyDeviationDistribution(
        int AcceleratingCount,
        int StableCount,
        int DeceleratingCount,
        int StalledCount,
        int NewCount);

    /// <summary>Métricas de desvio de frequência de deployment de um serviço.</summary>
    public sealed record ServiceFrequencyDeviationEntry(
        string ServiceName,
        int RecentReleasesCount,
        int HistoricalReleasesCount,
        decimal DeploysPerDayRecent,
        decimal DeploysPerDayHistorical,
        decimal DeviationPct,
        decimal RecentSuccessRatePct,
        FrequencyDeviation Deviation,
        bool RiskFlag);

    /// <summary>Resultado do relatório de desvio de frequência de releases.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int RecentDays,
        int HistoricalDays,
        int TotalServicesAnalyzed,
        FrequencyDeviationDistribution DeviationDistribution,
        IReadOnlyList<ServiceFrequencyDeviationEntry> TopAcceleratingServices,
        IReadOnlyList<ServiceFrequencyDeviationEntry> TopDeceleratingServices,
        IReadOnlyList<ServiceFrequencyDeviationEntry> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.RecentDays).InclusiveBetween(7, 60);
            RuleFor(q => q.HistoricalDays).GreaterThan(q => q.RecentDays);
            RuleFor(q => q.HistoricalDays).InclusiveBetween(30, 365);
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 200);
            RuleFor(q => q.Environment).MaximumLength(100).When(q => q.Environment is not null);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IReleaseRepository _releaseRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IReleaseRepository releaseRepo,
            IDateTimeProvider clock)
        {
            _releaseRepo = Guard.Against.Null(releaseRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var recentFrom = now.AddDays(-query.RecentDays);
            var historicalFrom = recentFrom.AddDays(-query.HistoricalDays);
            var tenantId = Guid.Parse(query.TenantId);

            // 1. Fetch releases for the full window (historical + recent)
            var recentReleases = await _releaseRepo.ListInRangeAsync(
                recentFrom, now, query.Environment, tenantId, cancellationToken);

            var historicalReleases = await _releaseRepo.ListInRangeAsync(
                historicalFrom, recentFrom, query.Environment, tenantId, cancellationToken);

            if (recentReleases.Count == 0 && historicalReleases.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    GeneratedAt: now,
                    RecentDays: query.RecentDays,
                    HistoricalDays: query.HistoricalDays,
                    TotalServicesAnalyzed: 0,
                    DeviationDistribution: new FrequencyDeviationDistribution(0, 0, 0, 0, 0),
                    TopAcceleratingServices: [],
                    TopDeceleratingServices: [],
                    AllServices: []));
            }

            // 2. Group releases by service
            var recentByService = recentReleases
                .GroupBy(r => r.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList(),
                    StringComparer.OrdinalIgnoreCase);

            var historicalByService = historicalReleases
                .GroupBy(r => r.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count(),
                    StringComparer.OrdinalIgnoreCase);

            // 3. Union of all service names from both windows
            var allServiceNames = new HashSet<string>(
                recentByService.Keys.Concat(historicalByService.Keys),
                StringComparer.OrdinalIgnoreCase);

            var entries = new List<ServiceFrequencyDeviationEntry>(allServiceNames.Count);

            foreach (var svcName in allServiceNames.OrderBy(n => n))
            {
                recentByService.TryGetValue(svcName, out var recentList);
                recentList ??= [];

                historicalByService.TryGetValue(svcName, out var historicalCount);

                int recentCount = recentList.Count;
                decimal recentPerDay = Math.Round((decimal)recentCount / query.RecentDays, 4);
                decimal historicalPerDay = Math.Round((decimal)historicalCount / query.HistoricalDays, 4);

                decimal deviationPct = ComputeDeviation(recentPerDay, historicalPerDay);
                var deviation = ClassifyDeviation(recentCount, historicalCount, deviationPct);

                // Success rate in recent window (Succeeded / total that are terminal)
                decimal recentSuccessRate = ComputeSuccessRate(recentList);

                bool riskFlag = deviation == FrequencyDeviation.Accelerating
                    && recentSuccessRate < RiskSuccessRateThreshold
                    || deviation == FrequencyDeviation.Stalled;

                entries.Add(new ServiceFrequencyDeviationEntry(
                    ServiceName: svcName,
                    RecentReleasesCount: recentCount,
                    HistoricalReleasesCount: historicalCount,
                    DeploysPerDayRecent: recentPerDay,
                    DeploysPerDayHistorical: historicalPerDay,
                    DeviationPct: deviationPct,
                    RecentSuccessRatePct: recentSuccessRate,
                    Deviation: deviation,
                    RiskFlag: riskFlag));
            }

            int accelCount = entries.Count(e => e.Deviation == FrequencyDeviation.Accelerating);
            int stableCount = entries.Count(e => e.Deviation == FrequencyDeviation.Stable);
            int decelCount = entries.Count(e => e.Deviation == FrequencyDeviation.Decelerating);
            int stalledCount = entries.Count(e => e.Deviation == FrequencyDeviation.Stalled);
            int newCount = entries.Count(e => e.Deviation == FrequencyDeviation.New);

            var topAccelerating = entries
                .Where(e => e.Deviation == FrequencyDeviation.Accelerating
                         || e.Deviation == FrequencyDeviation.New)
                .OrderByDescending(e => e.DeviationPct)
                .Take(query.MaxTopServices)
                .ToList();

            var topDecelerating = entries
                .Where(e => e.Deviation == FrequencyDeviation.Decelerating
                         || e.Deviation == FrequencyDeviation.Stalled)
                .OrderBy(e => e.DeviationPct)
                .Take(query.MaxTopServices)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                RecentDays: query.RecentDays,
                HistoricalDays: query.HistoricalDays,
                TotalServicesAnalyzed: entries.Count,
                DeviationDistribution: new FrequencyDeviationDistribution(accelCount, stableCount, decelCount, stalledCount, newCount),
                TopAcceleratingServices: topAccelerating,
                TopDeceleratingServices: topDecelerating,
                AllServices: entries));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static decimal ComputeDeviation(decimal recentPerDay, decimal historicalPerDay)
        {
            if (historicalPerDay == 0m)
                return recentPerDay > 0m ? 100m : 0m; // new service or no activity
            return Math.Round((recentPerDay - historicalPerDay) / historicalPerDay * 100m, 1);
        }

        private static FrequencyDeviation ClassifyDeviation(int recentCount, int historicalCount, decimal deviationPct)
        {
            if (recentCount == 0 && historicalCount > 0)
                return FrequencyDeviation.Stalled;
            if (historicalCount == 0 && recentCount > 0)
                return FrequencyDeviation.New;
            if (deviationPct > AcceleratingThreshold)
                return FrequencyDeviation.Accelerating;
            if (deviationPct < DeceleratingThreshold)
                return FrequencyDeviation.Decelerating;
            return FrequencyDeviation.Stable;
        }

        private static decimal ComputeSuccessRate(IReadOnlyList<Release> releases)
        {
            if (releases.Count == 0) return 100m;

            var terminal = releases.Where(r =>
                r.Status is DeploymentStatus.Succeeded
                    or DeploymentStatus.Failed
                    or DeploymentStatus.RolledBack).ToList();

            if (terminal.Count == 0) return 100m; // all in-progress → optimistic

            int succeeded = terminal.Count(r => r.Status == DeploymentStatus.Succeeded);
            return Math.Round((decimal)succeeded / terminal.Count * 100m, 1);
        }
    }
}
