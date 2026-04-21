using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetRuntimeBaselineComparisonReport;

/// <summary>
/// Feature: GetRuntimeBaselineComparisonReport — comparação de snapshots de runtime contra baselines.
///
/// Para cada par (serviço, ambiente) que possui snaphot recente, busca a baseline estabelecida
/// e calcula o desvio percentual de cada métrica (latência média, latência P99, taxa de erro,
/// throughput). Classifica o desvio em:
/// - <c>None</c> — desvio &lt; MinorThresholdPct em todas as métricas
/// - <c>Minor</c> — desvio 5–15% em pelo menos uma métrica
/// - <c>Moderate</c> — desvio 15–30% em pelo menos uma métrica
/// - <c>Severe</c> — desvio &gt; 30% em pelo menos uma métrica
///
/// Produz:
/// - totais (monitorados, com baseline, sem baseline, com drift, com drift severo)
/// - distribuição por severidade de drift
/// - top serviços com maior desvio composto
/// - desvio médio global de latência e taxa de erro
///
/// Serve Engineer, Tech Lead e Platform Admin como painel de alerta precoce de degradação.
///
/// Wave Q.1 — Runtime Baseline Comparison Report (OperationalIntelligence Runtime).
/// </summary>
public static class GetRuntimeBaselineComparisonReport
{
    /// <summary>
    /// <para><c>LookbackHours</c>: janela temporal em horas para selecionar snapshots recentes (1–168, default 48).</para>
    /// <para><c>MaxTopServices</c>: número máximo de serviços no ranking de drift (1–100, default 10).</para>
    /// <para><c>MinorDriftThresholdPct</c>: limiar mínimo de desvio percentual para classificar como Minor (1–30, default 5).</para>
    /// <para><c>Environment</c>: filtro opcional de ambiente (null = todos).</para>
    /// </summary>
    public sealed record Query(
        int LookbackHours = 48,
        int MaxTopServices = 10,
        int MinorDriftThresholdPct = 5,
        string? Environment = null) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Classificação de severidade de drift em relação à baseline.</summary>
    public enum DriftSeverity
    {
        /// <summary>Sem desvio significativo — dentro do limiar mínimo.</summary>
        None,
        /// <summary>Desvio leve — entre MinorThreshold e 15% de desvio.</summary>
        Minor,
        /// <summary>Desvio moderado — entre 15% e 30% de desvio.</summary>
        Moderate,
        /// <summary>Desvio severo — acima de 30% de desvio em pelo menos uma métrica.</summary>
        Severe
    }

    /// <summary>Distribuição de pares (serviço, ambiente) por severidade de drift.</summary>
    public sealed record DriftSeverityDistribution(
        int NoneCount,
        int MinorCount,
        int ModerateCount,
        int SevereCount);

    /// <summary>Métricas de desvio de um serviço em relação à sua baseline.</summary>
    public sealed record ServiceDriftEntry(
        string ServiceName,
        string Environment,
        DriftSeverity Severity,
        decimal AvgLatencyDeviationPct,
        decimal P99LatencyDeviationPct,
        decimal ErrorRateDeviationPct,
        decimal ThroughputDeviationPct,
        decimal CompositeDeviationPct,
        decimal BaselineConfidenceScore);

    /// <summary>Resultado do relatório de comparação de runtime com baseline.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        int LookbackHours,
        int TotalServicesMonitored,
        int ServicesWithBaseline,
        int ServicesWithoutBaseline,
        int ServicesWithDrift,
        int SevereDriftCount,
        decimal TenantAvgLatencyDeviationPct,
        decimal TenantAvgErrorRateDeviationPct,
        DriftSeverityDistribution SeverityDistribution,
        IReadOnlyList<ServiceDriftEntry> TopDriftingServices);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.LookbackHours).InclusiveBetween(1, 168);
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 100);
            RuleFor(q => q.MinorDriftThresholdPct).InclusiveBetween(1, 30);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private const decimal ModerateDriftThresholdPct = 15m;
        private const decimal SevereDriftThresholdPct = 30m;

        private readonly IRuntimeSnapshotRepository _snapshotRepo;
        private readonly IRuntimeBaselineRepository _baselineRepo;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IRuntimeSnapshotRepository snapshotRepo,
            IRuntimeBaselineRepository baselineRepo,
            IDateTimeProvider clock)
        {
            _snapshotRepo = Guard.Against.Null(snapshotRepo);
            _baselineRepo = Guard.Against.Null(baselineRepo);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = _clock.UtcNow;
            var since = now.AddHours(-query.LookbackHours);

            var pairs = await _snapshotRepo.GetServicesWithRecentSnapshotsAsync(since, cancellationToken);

            if (!string.IsNullOrWhiteSpace(query.Environment))
                pairs = pairs.Where(p => p.Environment.Equals(query.Environment, StringComparison.OrdinalIgnoreCase)).ToList();

            var driftEntries = new List<ServiceDriftEntry>();
            int servicesWithBaseline = 0;
            int servicesWithoutBaseline = 0;

            foreach (var (serviceName, environment) in pairs)
            {
                var baseline = await _baselineRepo.GetByServiceAndEnvironmentAsync(serviceName, environment, cancellationToken);
                if (baseline is null)
                {
                    servicesWithoutBaseline++;
                    continue;
                }

                var snapshot = await _snapshotRepo.GetLatestByServiceAsync(serviceName, environment, cancellationToken);
                if (snapshot is null)
                {
                    servicesWithoutBaseline++;
                    continue;
                }

                servicesWithBaseline++;

                var avgLatencyDev = ComputeDeviation(snapshot.AvgLatencyMs, baseline.ExpectedAvgLatencyMs);
                var p99LatencyDev = ComputeDeviation(snapshot.P99LatencyMs, baseline.ExpectedP99LatencyMs);
                var errorRateDev = ComputeDeviation(snapshot.ErrorRate, baseline.ExpectedErrorRate);
                var throughputDev = ComputeDeviation(snapshot.RequestsPerSecond, baseline.ExpectedRequestsPerSecond);

                // CompositeDeviationPct is the maximum deviation across all metrics —
                // any single metric exceeding the threshold is sufficient to trigger an alert.
                var composite = Math.Max(Math.Max(avgLatencyDev, p99LatencyDev), Math.Max(errorRateDev, throughputDev));
                var severity = ClassifySeverity(composite, query.MinorDriftThresholdPct);
                driftEntries.Add(new ServiceDriftEntry(
                    ServiceName: serviceName,
                    Environment: environment,
                    Severity: severity,
                    AvgLatencyDeviationPct: Math.Round(avgLatencyDev, 2),
                    P99LatencyDeviationPct: Math.Round(p99LatencyDev, 2),
                    ErrorRateDeviationPct: Math.Round(errorRateDev, 2),
                    ThroughputDeviationPct: Math.Round(throughputDev, 2),
                    CompositeDeviationPct: Math.Round(composite, 2),
                    BaselineConfidenceScore: baseline.ConfidenceScore));
            }

            int noneCount = driftEntries.Count(e => e.Severity == DriftSeverity.None);
            int minorCount = driftEntries.Count(e => e.Severity == DriftSeverity.Minor);
            int moderateCount = driftEntries.Count(e => e.Severity == DriftSeverity.Moderate);
            int severeCount = driftEntries.Count(e => e.Severity == DriftSeverity.Severe);
            int servicesWithDrift = minorCount + moderateCount + severeCount;

            var topDrifting = driftEntries
                .OrderByDescending(e => e.CompositeDeviationPct)
                .Take(query.MaxTopServices)
                .ToList();

            decimal avgLatencyDiff = driftEntries.Count > 0
                ? Math.Round(driftEntries.Average(e => e.AvgLatencyDeviationPct), 2)
                : 0m;

            decimal avgErrorDiff = driftEntries.Count > 0
                ? Math.Round(driftEntries.Average(e => e.ErrorRateDeviationPct), 2)
                : 0m;

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                LookbackHours: query.LookbackHours,
                TotalServicesMonitored: pairs.Count,
                ServicesWithBaseline: servicesWithBaseline,
                ServicesWithoutBaseline: servicesWithoutBaseline,
                ServicesWithDrift: servicesWithDrift,
                SevereDriftCount: severeCount,
                TenantAvgLatencyDeviationPct: avgLatencyDiff,
                TenantAvgErrorRateDeviationPct: avgErrorDiff,
                SeverityDistribution: new DriftSeverityDistribution(noneCount, minorCount, moderateCount, severeCount),
                TopDriftingServices: topDrifting));
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static decimal ComputeDeviation(decimal actual, decimal expected)
        {
            if (expected == 0m) return actual == 0m ? 0m : 100m;
            return Math.Abs((actual - expected) / expected) * 100m;
        }

        private static DriftSeverity ClassifySeverity(decimal compositeDevPct, int minorThresholdPct)
        {
            if (compositeDevPct >= SevereDriftThresholdPct) return DriftSeverity.Severe;
            if (compositeDevPct >= ModerateDriftThresholdPct) return DriftSeverity.Moderate;
            if (compositeDevPct >= minorThresholdPct) return DriftSeverity.Minor;
            return DriftSeverity.None;
        }
    }
}
