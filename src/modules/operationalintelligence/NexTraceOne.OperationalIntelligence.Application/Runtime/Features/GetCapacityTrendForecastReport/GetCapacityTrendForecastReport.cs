using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetCapacityTrendForecastReport;

/// <summary>
/// Feature: GetCapacityTrendForecastReport — projecção de tendências de capacidade por serviço.
///
/// Aplica regressão linear simples sobre séries temporais de <c>RuntimeSnapshot</c>
/// (AvgLatencyMs, RequestsPerSecond, ErrorRate) por serviço e ambiente, extrapolando
/// para os próximos 30 dias.
///
/// Para cada serviço com ≥ <c>MinDataPoints</c> snapshots, calcula:
/// - <c>LatencyTrend</c>                   — slope de AvgLatencyMs (positivo = degradação)
/// - <c>ThroughputTrend</c>                — slope de RequestsPerSecond (positivo = crescimento)
/// - <c>ErrorRateTrend</c>                 — slope de ErrorRate
/// - <c>ProjectedLatencyIn30DaysMs</c>     — extrapolação de latência
/// - <c>ProjectedThroughputIn30Days</c>    — extrapolação de throughput
/// - <c>DaysToLatencyThreshold</c>         — dias até AvgLatencyMs exceder threshold crítico
/// - <c>DaysToErrorRateThreshold</c>       — dias até ErrorRate exceder threshold crítico
///
/// Classifica por <c>ForecastAlertTier</c>:
/// - <c>Stable</c>    — nenhum threshold atingido em 90 dias com tendência actual
/// - <c>WatchList</c> — threshold estimado em 31–90 dias
/// - <c>AtRisk</c>    — threshold estimado em 8–30 dias
/// - <c>Imminent</c>  — threshold estimado em ≤ 7 dias (acção imediata recomendada)
///
/// Produz:
/// - lista de serviços por tier de alerta
/// - <c>TenantCapacitySummary</c> — distribuição % por ForecastAlertTier
/// - top serviços por menor DaysToThreshold
///
/// Orientado para Architect, Platform Admin e Engineer.
///
/// Wave AI.2 — Capacity Trend Forecast Report (OperationalIntelligence Runtime).
/// </summary>
public static class GetCapacityTrendForecastReport
{
    // ── Default thresholds ──────────────────────────────────────────────────
    private const decimal DefaultLatencyCriticalMs = 2000m;
    private const decimal DefaultErrorRateCriticalPct = 5m;
    private const int DefaultMinDataPoints = 14;
    private const int ProjectionDays = 30;
    private const int WatchListDays = 90;
    private const int AtRiskDays = 30;
    private const int ImminentDays = 7;
    private const int PageSize = 500;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente (null = todos).</para>
    /// <para><c>LookbackDays</c>: janela histórica para regressão (14–180, default 60).</para>
    /// <para><c>MinDataPoints</c>: mínimo de snapshots para habilitar projecção (3–60, default 14).</para>
    /// <para><c>LatencyCriticalMs</c>: threshold de latência para alerta (default 2000ms).</para>
    /// <para><c>ErrorRateCriticalPct</c>: threshold de error rate % para alerta (default 5%).</para>
    /// <para><c>MaxTopServices</c>: máximo de serviços nos rankings (1–100, default 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        string? Environment = null,
        int LookbackDays = 60,
        int MinDataPoints = DefaultMinDataPoints,
        decimal LatencyCriticalMs = DefaultLatencyCriticalMs,
        decimal ErrorRateCriticalPct = DefaultErrorRateCriticalPct,
        int MaxTopServices = 10) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Nível de alerta de capacidade baseado no tempo estimado até threshold crítico.</summary>
    public enum ForecastAlertTier
    {
        /// <summary>Nenhum threshold vai ser atingido em 90 dias com tendência actual.</summary>
        Stable,
        /// <summary>Threshold estimado em 31–90 dias — monitorar regularmente.</summary>
        WatchList,
        /// <summary>Threshold estimado em 8–30 dias — atenção e acção necessárias.</summary>
        AtRisk,
        /// <summary>Threshold estimado em ≤ 7 dias — acção imediata recomendada.</summary>
        Imminent
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Distribuição de serviços por tier de alerta de capacidade.</summary>
    public sealed record TierDistribution(
        int StableCount,
        int WatchListCount,
        int AtRiskCount,
        int ImminentCount);

    /// <summary>Entrada de projecção de capacidade para um serviço.</summary>
    public sealed record ServiceCapacityForecast(
        string ServiceName,
        string Environment,
        int DataPointsUsed,
        decimal CurrentAvgLatencyMs,
        decimal CurrentErrorRatePct,
        decimal CurrentRequestsPerSecond,
        decimal LatencyTrendSlopePerDay,
        decimal ThroughputTrendSlopePerDay,
        decimal ErrorRateTrendSlopePerDay,
        decimal ProjectedLatencyIn30DaysMs,
        decimal ProjectedThroughputIn30Days,
        int? DaysToLatencyThreshold,
        int? DaysToErrorRateThreshold,
        ForecastAlertTier AlertTier);

    /// <summary>Sumário de capacidade do tenant.</summary>
    public sealed record TenantCapacitySummary(
        int TotalServicesAnalyzed,
        int ServicesWithInsufficientData,
        TierDistribution TierDistribution,
        decimal ImminentPct,
        decimal AtRiskPct,
        decimal WatchListPct,
        decimal StablePct);

    /// <summary>Resultado do relatório de projecção de capacidade.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string? Environment,
        int LookbackDays,
        TenantCapacitySummary TenantCapacitySummary,
        IReadOnlyList<ServiceCapacityForecast> TopUrgentServices,
        IReadOnlyList<ServiceCapacityForecast> AllServices);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(14, 180);
            RuleFor(q => q.MinDataPoints).InclusiveBetween(3, 60);
            RuleFor(q => q.LatencyCriticalMs).GreaterThan(0m);
            RuleFor(q => q.ErrorRateCriticalPct).InclusiveBetween(0.1m, 100m);
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IRuntimeSnapshotRepository _snapshotRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IRuntimeSnapshotRepository snapshotRepo,
            IDateTimeProvider clock)
        {
            _snapshotRepo = Guard.Against.Null(snapshotRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var since = now.AddDays(-query.LookbackDays);

            // 1. Discover all service/environment pairs with recent snapshots
            var allPairs = await _snapshotRepo.GetServicesWithRecentSnapshotsAsync(
                since, cancellationToken);

            // 2. Filter by environment if requested
            var filteredPairs = query.Environment is null
                ? allPairs
                : allPairs.Where(p =>
                    string.Equals(p.Environment, query.Environment, StringComparison.OrdinalIgnoreCase))
                  .ToList();

            if (filteredPairs.Count == 0)
            {
                return Result<Report>.Success(EmptyReport(now, query));
            }

            // 3. Build per-service forecasts
            var forecasts = new List<ServiceCapacityForecast>();
            int insufficientDataCount = 0;

            foreach (var (serviceName, environment) in filteredPairs)
            {
                var snapshots = await _snapshotRepo.ListByServiceAsync(
                    serviceName, environment, page: 1, pageSize: PageSize, cancellationToken);

                var inWindow = snapshots
                    .Where(s => s.CapturedAt >= since)
                    .OrderBy(s => s.CapturedAt)
                    .ToList();

                if (inWindow.Count < query.MinDataPoints)
                {
                    insufficientDataCount++;
                    continue;
                }

                var forecast = BuildForecast(serviceName, environment, inWindow, query, now);
                forecasts.Add(forecast);
            }

            // 4. Sort and build summary
            var allSorted = forecasts
                .OrderBy(f => (int)f.AlertTier == 0 ? int.MaxValue : f.DaysToLatencyThreshold ?? f.DaysToErrorRateThreshold ?? int.MaxValue)
                .ThenByDescending(f => (int)f.AlertTier)
                .ThenBy(f => f.ServiceName)
                .ToList();

            var topUrgent = forecasts
                .Where(f => f.AlertTier != ForecastAlertTier.Stable)
                .OrderByDescending(f => (int)f.AlertTier)
                .ThenBy(f => f.DaysToLatencyThreshold ?? f.DaysToErrorRateThreshold ?? int.MaxValue)
                .Take(query.MaxTopServices)
                .ToList();

            var summary = BuildSummary(forecasts, insufficientDataCount);

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                Environment: query.Environment,
                LookbackDays: query.LookbackDays,
                TenantCapacitySummary: summary,
                TopUrgentServices: topUrgent,
                AllServices: allSorted));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static ServiceCapacityForecast BuildForecast(
            string serviceName,
            string environment,
            IReadOnlyList<Domain.Runtime.Entities.RuntimeSnapshot> snapshots,
            Query query,
            DateTimeOffset now)
        {
            // Convert to time-indexed arrays (x = days from first snapshot)
            var t0 = snapshots[0].CapturedAt;
            var xs = snapshots.Select(s => (decimal)(s.CapturedAt - t0).TotalDays).ToArray();
            var latencies = snapshots.Select(s => s.AvgLatencyMs).ToArray();
            var throughputs = snapshots.Select(s => s.RequestsPerSecond).ToArray();
            var errorRates = snapshots.Select(s => s.ErrorRate).ToArray();

            decimal latencySlope = LinearRegressionSlope(xs, latencies);
            decimal throughputSlope = LinearRegressionSlope(xs, throughputs);
            decimal errorRateSlope = LinearRegressionSlope(xs, errorRates);

            decimal currentLatency = snapshots[^1].AvgLatencyMs;
            decimal currentThroughput = snapshots[^1].RequestsPerSecond;
            decimal currentErrorRate = snapshots[^1].ErrorRate;

            decimal projLatency = Math.Max(0m, currentLatency + latencySlope * ProjectionDays);
            decimal projThroughput = Math.Max(0m, currentThroughput + throughputSlope * ProjectionDays);

            int? daysToLatency = null;
            int? daysToErrorRate = null;

            // Days to latency threshold
            if (latencySlope > 0m && currentLatency < query.LatencyCriticalMs)
            {
                decimal remaining = query.LatencyCriticalMs - currentLatency;
                int days = (int)Math.Ceiling((double)(remaining / latencySlope));
                daysToLatency = days > 0 ? days : null;
            }

            // Days to error rate threshold
            if (errorRateSlope > 0m && currentErrorRate < query.ErrorRateCriticalPct)
            {
                decimal remaining = query.ErrorRateCriticalPct - currentErrorRate;
                int days = (int)Math.Ceiling((double)(remaining / errorRateSlope));
                daysToErrorRate = days > 0 ? days : null;
            }

            int? minDays = MinNullable(daysToLatency, daysToErrorRate);
            var alertTier = ClassifyAlertTier(minDays);

            return new ServiceCapacityForecast(
                ServiceName: serviceName,
                Environment: environment,
                DataPointsUsed: snapshots.Count,
                CurrentAvgLatencyMs: Math.Round(currentLatency, 1),
                CurrentErrorRatePct: Math.Round(currentErrorRate, 3),
                CurrentRequestsPerSecond: Math.Round(currentThroughput, 2),
                LatencyTrendSlopePerDay: Math.Round(latencySlope, 4),
                ThroughputTrendSlopePerDay: Math.Round(throughputSlope, 4),
                ErrorRateTrendSlopePerDay: Math.Round(errorRateSlope, 6),
                ProjectedLatencyIn30DaysMs: Math.Round(projLatency, 1),
                ProjectedThroughputIn30Days: Math.Round(projThroughput, 2),
                DaysToLatencyThreshold: daysToLatency,
                DaysToErrorRateThreshold: daysToErrorRate,
                AlertTier: alertTier);
        }

        private static decimal LinearRegressionSlope(decimal[] xs, decimal[] ys)
        {
            int n = xs.Length;
            if (n < 2) return 0m;

            decimal xMean = xs.Average();
            decimal yMean = ys.Average();

            decimal numerator = 0m;
            decimal denominator = 0m;
            for (int i = 0; i < n; i++)
            {
                decimal dx = xs[i] - xMean;
                numerator += dx * (ys[i] - yMean);
                denominator += dx * dx;
            }

            return denominator == 0m ? 0m : numerator / denominator;
        }

        private static ForecastAlertTier ClassifyAlertTier(int? minDaysToThreshold) =>
            minDaysToThreshold switch
            {
                null => ForecastAlertTier.Stable,
                <= ImminentDays => ForecastAlertTier.Imminent,
                <= AtRiskDays => ForecastAlertTier.AtRisk,
                <= WatchListDays => ForecastAlertTier.WatchList,
                _ => ForecastAlertTier.Stable
            };

        private static int? MinNullable(int? a, int? b) =>
            (a, b) switch
            {
                (null, null) => null,
                (null, _) => b,
                (_, null) => a,
                _ => Math.Min(a.Value, b.Value)
            };

        private static TenantCapacitySummary BuildSummary(
            IReadOnlyList<ServiceCapacityForecast> forecasts,
            int insufficientDataCount)
        {
            int total = forecasts.Count;
            int stableCount = forecasts.Count(f => f.AlertTier == ForecastAlertTier.Stable);
            int watchCount = forecasts.Count(f => f.AlertTier == ForecastAlertTier.WatchList);
            int atRiskCount = forecasts.Count(f => f.AlertTier == ForecastAlertTier.AtRisk);
            int imminentCount = forecasts.Count(f => f.AlertTier == ForecastAlertTier.Imminent);

            decimal imminentPct = total > 0 ? Math.Round((decimal)imminentCount / total * 100m, 1) : 0m;
            decimal atRiskPct = total > 0 ? Math.Round((decimal)atRiskCount / total * 100m, 1) : 0m;
            decimal watchPct = total > 0 ? Math.Round((decimal)watchCount / total * 100m, 1) : 0m;
            decimal stablePct = total > 0 ? Math.Round((decimal)stableCount / total * 100m, 1) : 0m;

            return new TenantCapacitySummary(
                TotalServicesAnalyzed: total,
                ServicesWithInsufficientData: insufficientDataCount,
                TierDistribution: new TierDistribution(stableCount, watchCount, atRiskCount, imminentCount),
                ImminentPct: imminentPct,
                AtRiskPct: atRiskPct,
                WatchListPct: watchPct,
                StablePct: stablePct);
        }

        private static Report EmptyReport(DateTimeOffset now, Query query) => new(
            GeneratedAt: now,
            Environment: query.Environment,
            LookbackDays: query.LookbackDays,
            TenantCapacitySummary: new TenantCapacitySummary(0, 0,
                new TierDistribution(0, 0, 0, 0), 0m, 0m, 0m, 0m),
            TopUrgentServices: [],
            AllServices: []);
    }
}
