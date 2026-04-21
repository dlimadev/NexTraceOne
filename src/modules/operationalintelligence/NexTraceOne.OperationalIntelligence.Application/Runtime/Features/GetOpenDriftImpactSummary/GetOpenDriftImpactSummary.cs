using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetOpenDriftImpactSummary;

/// <summary>
/// Feature: GetOpenDriftImpactSummary — sumário de impacto dos drift findings abertos.
///
/// Agrega todos os drift findings não reconhecidos e não resolvidos e produz:
/// - total de drifts abertos
/// - serviços mais afetados (ranking por contagem de drifts)
/// - métricas mais desviantes (por percentagem de desvio média)
/// - desvio médio global e máximo
/// - distribuição de severidade (Low/Medium/High/Critical) baseada no desvio percentual
///
/// Serve como painel de risco operacional para Tech Lead, Engineer e Platform Admin.
/// Complementa o GetOperationalReadinessReport com detalhe sobre os drifts abertos.
///
/// Wave M.3 — Open Drift Impact Summary (OperationalIntelligence Runtime).
/// </summary>
public static class GetOpenDriftImpactSummary
{
    /// <summary>
    /// <para><c>MaxServices</c>: máximo de serviços no ranking (1–100, default 10).</para>
    /// <para><c>MaxMetrics</c>: máximo de métricas desviantes no ranking (1–50, default 10).</para>
    /// <para><c>PageSize</c>: tamanho da página para busca de drifts (10–500, default 200).</para>
    /// </summary>
    public sealed record Query(
        int MaxServices = 10,
        int MaxMetrics = 10,
        int PageSize = 200) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Severidade do drift baseada no desvio percentual.</summary>
    public enum DriftSeverity
    {
        /// <summary>Desvio < 10%.</summary>
        Low,
        /// <summary>Desvio 10–30%.</summary>
        Medium,
        /// <summary>Desvio 30–60%.</summary>
        High,
        /// <summary>Desvio > 60%.</summary>
        Critical
    }

    /// <summary>Serviço com maior número de drifts abertos.</summary>
    public sealed record ServiceDriftEntry(
        string ServiceName,
        string Environment,
        int OpenDriftCount,
        decimal MaxDeviationPercent,
        decimal AvgDeviationPercent,
        DriftSeverity WorstSeverity);

    /// <summary>Métrica com maior desvio médio entre todos os serviços.</summary>
    public sealed record MetricDriftEntry(
        string MetricName,
        int AffectedServices,
        decimal AvgDeviationPercent,
        decimal MaxDeviationPercent);

    /// <summary>Distribuição de drifts por severidade.</summary>
    public sealed record SeverityDistribution(
        int LowCount,
        int MediumCount,
        int HighCount,
        int CriticalCount);

    /// <summary>Resultado do sumário de impacto de drifts abertos.</summary>
    public sealed record Report(
        int TotalOpenDrifts,
        decimal GlobalAvgDeviationPercent,
        decimal GlobalMaxDeviationPercent,
        SeverityDistribution SeverityDistribution,
        IReadOnlyList<ServiceDriftEntry> TopAffectedServices,
        IReadOnlyList<MetricDriftEntry> TopDeviantMetrics,
        DateTimeOffset GeneratedAt);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.MaxServices).InclusiveBetween(1, 100);
            RuleFor(q => q.MaxMetrics).InclusiveBetween(1, 50);
            RuleFor(q => q.PageSize).InclusiveBetween(10, 500);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IDriftFindingRepository driftFindingRepository,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);

            var drifts = await driftFindingRepository
                .ListUnacknowledgedAsync(page: 1, pageSize: query.PageSize, cancellationToken);

            var openDrifts = drifts.Where(d => d.IsOpen).ToList();

            if (openDrifts.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    TotalOpenDrifts: 0,
                    GlobalAvgDeviationPercent: 0m,
                    GlobalMaxDeviationPercent: 0m,
                    SeverityDistribution: new SeverityDistribution(0, 0, 0, 0),
                    TopAffectedServices: [],
                    TopDeviantMetrics: [],
                    GeneratedAt: clock.UtcNow));
            }

            var globalAvg = Math.Round(openDrifts.Average(d => d.DeviationPercent), 2);
            var globalMax = openDrifts.Max(d => d.DeviationPercent);

            // Severity distribution
            var severityDist = new SeverityDistribution(
                LowCount: openDrifts.Count(d => d.DeviationPercent < 10m),
                MediumCount: openDrifts.Count(d => d.DeviationPercent >= 10m && d.DeviationPercent < 30m),
                HighCount: openDrifts.Count(d => d.DeviationPercent >= 30m && d.DeviationPercent <= 60m),
                CriticalCount: openDrifts.Count(d => d.DeviationPercent > 60m));

            // Top services by open drift count
            var topServices = openDrifts
                .GroupBy(d => (d.ServiceName, d.Environment))
                .Select(g =>
                {
                    var maxDev = g.Max(d => d.DeviationPercent);
                    var avgDev = Math.Round(g.Average(d => d.DeviationPercent), 2);
                    return new ServiceDriftEntry(
                        ServiceName: g.Key.ServiceName,
                        Environment: g.Key.Environment,
                        OpenDriftCount: g.Count(),
                        MaxDeviationPercent: maxDev,
                        AvgDeviationPercent: avgDev,
                        WorstSeverity: ClassifySeverity(maxDev));
                })
                .OrderByDescending(s => s.OpenDriftCount)
                .ThenByDescending(s => s.MaxDeviationPercent)
                .Take(query.MaxServices)
                .ToList();

            // Top deviant metrics across all services
            var topMetrics = openDrifts
                .GroupBy(d => d.MetricName)
                .Select(g => new MetricDriftEntry(
                    MetricName: g.Key,
                    AffectedServices: g.Select(d => d.ServiceName).Distinct().Count(),
                    AvgDeviationPercent: Math.Round(g.Average(d => d.DeviationPercent), 2),
                    MaxDeviationPercent: g.Max(d => d.DeviationPercent)))
                .OrderByDescending(m => m.AvgDeviationPercent)
                .Take(query.MaxMetrics)
                .ToList();

            return Result<Report>.Success(new Report(
                TotalOpenDrifts: openDrifts.Count,
                GlobalAvgDeviationPercent: globalAvg,
                GlobalMaxDeviationPercent: globalMax,
                SeverityDistribution: severityDist,
                TopAffectedServices: topServices,
                TopDeviantMetrics: topMetrics,
                GeneratedAt: clock.UtcNow));
        }

        private static DriftSeverity ClassifySeverity(decimal deviationPercent)
            => deviationPercent > 60m ? DriftSeverity.Critical
             : deviationPercent >= 30m ? DriftSeverity.High
             : deviationPercent >= 10m ? DriftSeverity.Medium
             : DriftSeverity.Low;
    }
}
