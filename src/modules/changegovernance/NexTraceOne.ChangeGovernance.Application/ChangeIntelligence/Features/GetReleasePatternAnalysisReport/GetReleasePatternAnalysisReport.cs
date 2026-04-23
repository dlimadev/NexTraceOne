using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleasePatternAnalysisReport;

/// <summary>
/// Feature: GetReleasePatternAnalysisReport — análise sistémica de padrões de release por tenant.
///
/// Agrega releases de um tenant num período e produz:
/// - análise de batch size (tamanho de release vs taxa de falha)
/// - padrões temporais de risco (fins de semana, fim de sprint, heat-map por hora e dia)
/// - risco de clustering (releases em simultâneo no mesmo dia e ambiente)
/// - padrões de incidente pós-release (em 1h e 24h, serviços com falhas repetidas)
/// - score composto de padrão de release do tenant (0–100)
///
/// Wave AW.1 — Release Pattern Analysis Report (ChangeGovernance ChangeIntelligence).
/// </summary>
public static class GetReleasePatternAnalysisReport
{
    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–365, default 90).</para>
    /// <para><c>LargeReleaseThreshold</c>: número de serviços acima do qual a release é considerada grande (default 5).</para>
    /// <para><c>RepeatFailureThreshold</c>: taxa de falha em hora 1 acima da qual o serviço é considerado com falha repetida (default 0.3).</para>
    /// <para><c>ClusterWarningPerWeek</c>: número de dias com clustering que dispara aviso semanal (default 3).</para>
    /// <para><c>EndOfSprintDays</c>: número de dias antes do fim de sprint que qualifica como end-of-sprint (default 3).</para>
    /// <para><c>MaxServices</c>: número máximo de serviços no relatório (1–200, default 20).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int LargeReleaseThreshold = 5,
        decimal RepeatFailureThreshold = 0.3m,
        int ClusterWarningPerWeek = 3,
        int EndOfSprintDays = 3,
        int MaxServices = 20) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Tier de risco de clustering de releases.</summary>
    public enum ReleaseClusteringTier
    {
        /// <summary>Clustering semanal dentro do limite configurado.</summary>
        Safe,
        /// <summary>Clustering semanal ligeiramente acima do limite.</summary>
        Warning,
        /// <summary>Clustering semanal significativamente elevado.</summary>
        Risky,
        /// <summary>Clustering semanal crítico — risco operacional elevado.</summary>
        Critical
    }

    /// <summary>Análise de batch size das releases.</summary>
    public sealed record BatchSizeAnalysisResult(
        decimal AvgServiceChangesPerRelease,
        int LargeReleaseCount,
        bool BatchSizeVsFailureCorrelationSignificant,
        string BatchSizeTrend);

    /// <summary>Padrões temporais de risco nas releases.</summary>
    public sealed record TemporalPatternsResult(
        decimal HighRiskDayConcentrationPct,
        decimal EndOfSprintClusterPct,
        IReadOnlyDictionary<string, int> DeploymentHeatmapByHourBucket,
        IReadOnlyDictionary<string, int> DeploymentHeatmapByDayOfWeek);

    /// <summary>Risco de clustering de múltiplas releases no mesmo dia.</summary>
    public sealed record ClusteringRiskResult(
        int MultiServiceSameDayReleases,
        int MaxDailyReleaseCount,
        ReleaseClusteringTier Tier);

    /// <summary>Padrões de incidente após release.</summary>
    public sealed record IncidentPatternResult(
        decimal IncidentInHour1Rate,
        decimal IncidentInDay1Rate,
        IReadOnlyList<string> RepeatFailureServices);

    /// <summary>Resultado do relatório de análise de padrões de release.</summary>
    public sealed record Report(
        BatchSizeAnalysisResult BatchSizeAnalysis,
        TemporalPatternsResult TemporalPatterns,
        ClusteringRiskResult ClusteringRisk,
        IncidentPatternResult IncidentPatterns,
        decimal TenantReleasePatternScore,
        string TenantId,
        int LookbackDays,
        DateTimeOffset From,
        DateTimeOffset To,
        DateTimeOffset GeneratedAt);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.MaxServices).InclusiveBetween(1, 200);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IReleasePatternReader reader,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var to = clock.UtcNow;
            var from = to.AddDays(-query.LookbackDays);

            var releases = await reader.ListReleasesByTenantAsync(
                query.TenantId, from, to, cancellationToken);

            var total = releases.Count;

            if (total == 0)
            {
                return Result<Report>.Success(new Report(
                    BatchSizeAnalysis: new BatchSizeAnalysisResult(0m, 0, false, "Stable"),
                    TemporalPatterns: new TemporalPatternsResult(0m, 0m,
                        BuildEmptyHeatmapByHour(), BuildEmptyHeatmapByDay()),
                    ClusteringRisk: new ClusteringRiskResult(0, 0, ReleaseClusteringTier.Safe),
                    IncidentPatterns: new IncidentPatternResult(0m, 0m, []),
                    TenantReleasePatternScore: 100m,
                    TenantId: query.TenantId,
                    LookbackDays: query.LookbackDays,
                    From: from,
                    To: to,
                    GeneratedAt: clock.UtcNow));
            }

            // ── Batch Size Analysis ───────────────────────────────────────────

            var avgChanges = Math.Round((decimal)releases.Sum(r => r.ServiceChangesCount) / total, 2);
            var largeReleases = releases.Where(r => r.ServiceChangesCount > query.LargeReleaseThreshold).ToList();
            var largeReleaseCount = largeReleases.Count;

            var globalFailureRate = total > 0 ? (decimal)releases.Count(r => r.HasIncident) / total : 0m;
            var largeReleaseFailureRate = largeReleaseCount > 0
                ? (decimal)largeReleases.Count(r => r.HasIncident) / largeReleaseCount
                : 0m;
            var batchSizeVsFailureSignificant = largeReleaseFailureRate > globalFailureRate * 1.5m;

            var midPoint = from.AddDays(query.LookbackDays / 2.0);
            var firstHalf = releases.Where(r => r.DeployedAt < midPoint).ToList();
            var secondHalf = releases.Where(r => r.DeployedAt >= midPoint).ToList();
            var firstHalfAvg = firstHalf.Count > 0 ? (decimal)firstHalf.Sum(r => r.ServiceChangesCount) / firstHalf.Count : 0m;
            var secondHalfAvg = secondHalf.Count > 0 ? (decimal)secondHalf.Sum(r => r.ServiceChangesCount) / secondHalf.Count : 0m;
            var batchSizeTrend = secondHalfAvg > firstHalfAvg * 1.1m ? "Increasing"
                : secondHalfAvg < firstHalfAvg * 0.9m ? "Decreasing"
                : "Stable";

            var batchSizeAnalysis = new BatchSizeAnalysisResult(
                AvgServiceChangesPerRelease: avgChanges,
                LargeReleaseCount: largeReleaseCount,
                BatchSizeVsFailureCorrelationSignificant: batchSizeVsFailureSignificant,
                BatchSizeTrend: batchSizeTrend);

            // ── Temporal Patterns ─────────────────────────────────────────────

            var highRiskDayCount = releases.Count(r =>
                r.DeployedAt.DayOfWeek == DayOfWeek.Saturday ||
                r.DeployedAt.DayOfWeek == DayOfWeek.Sunday ||
                r.DeployedAt.DayOfWeek == DayOfWeek.Friday);
            var highRiskDayPct = Math.Round((decimal)highRiskDayCount / total * 100m, 2);
            var endOfSprintPct = Math.Round((decimal)releases.Count(r => r.IsEndOfSprint) / total * 100m, 2);

            var heatmapByHour = releases.GroupBy(r => HourBucket(r.DeployedAt.Hour))
                .ToDictionary(g => g.Key, g => g.Count());
            foreach (var bucket in new[] { "0-5", "6-11", "12-17", "18-23" })
                heatmapByHour.TryAdd(bucket, 0);

            var heatmapByDay = releases.GroupBy(r => r.DeployedAt.DayOfWeek.ToString())
                .ToDictionary(g => g.Key, g => g.Count());
            foreach (var day in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
                heatmapByDay.TryAdd(day, 0);

            var temporalPatterns = new TemporalPatternsResult(
                HighRiskDayConcentrationPct: highRiskDayPct,
                EndOfSprintClusterPct: endOfSprintPct,
                DeploymentHeatmapByHourBucket: heatmapByHour,
                DeploymentHeatmapByDayOfWeek: heatmapByDay);

            // ── Clustering Risk ───────────────────────────────────────────────

            var releasesByDayAndEnv = releases
                .GroupBy(r => (r.DeployedAt.Date, r.Environment), (k, g) => g.Count())
                .ToList();
            var clusterDays = releasesByDayAndEnv.Count(c => c > 3);
            var maxDailyCount = releasesByDayAndEnv.Count > 0 ? releasesByDayAndEnv.Max() : 0;

            var weeksInPeriod = Math.Max(1.0, query.LookbackDays / 7.0);
            var clusteringPerWeek = clusterDays / weeksInPeriod;
            var clusteringTier = ClassifyClusteringTier(clusteringPerWeek, query.ClusterWarningPerWeek);

            var clusteringRisk = new ClusteringRiskResult(
                MultiServiceSameDayReleases: clusterDays,
                MaxDailyReleaseCount: maxDailyCount,
                Tier: clusteringTier);

            // ── Incident Pattern After Release ────────────────────────────────

            var incidentInHour1 = releases.Count(r => r.HasIncident && r.IncidentAt.HasValue
                && r.IncidentAt.Value <= r.DeployedAt.AddHours(1));
            var incidentInDay1 = releases.Count(r => r.HasIncident && r.IncidentAt.HasValue
                && r.IncidentAt.Value <= r.DeployedAt.AddHours(24));

            var incidentHour1Rate = Math.Round((decimal)incidentInHour1 / total, 4);
            var incidentDay1Rate = Math.Round((decimal)incidentInDay1 / total, 4);

            var repeatFailureServices = releases
                .GroupBy(r => r.ServiceName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var svcTotal = g.Count();
                    var svcHour1 = g.Count(r => r.HasIncident && r.IncidentAt.HasValue
                        && r.IncidentAt.Value <= r.DeployedAt.AddHours(1));
                    var rate = svcTotal > 0 ? (decimal)svcHour1 / svcTotal : 0m;
                    return (ServiceName: g.Key, Rate: rate);
                })
                .Where(x => x.Rate > query.RepeatFailureThreshold)
                .OrderByDescending(x => x.Rate)
                .Take(query.MaxServices)
                .Select(x => x.ServiceName)
                .ToList();

            var incidentPatterns = new IncidentPatternResult(
                IncidentInHour1Rate: incidentHour1Rate,
                IncidentInDay1Rate: incidentDay1Rate,
                RepeatFailureServices: repeatFailureServices);

            // ── Score Composite (0–100) ───────────────────────────────────────

            var batchSizeRisk = ComputeBatchSizeRisk(batchSizeVsFailureSignificant, largeReleaseCount, total);
            var temporalRisk = ComputeTemporalRisk(highRiskDayPct, endOfSprintPct);
            var clusterRisk = ComputeClusteringRisk(clusteringTier);
            var incidentRisk = ComputeIncidentRisk(incidentHour1Rate);

            var score = Math.Round(Math.Max(0m, 100m - batchSizeRisk - temporalRisk - clusterRisk - incidentRisk), 1);

            return Result<Report>.Success(new Report(
                BatchSizeAnalysis: batchSizeAnalysis,
                TemporalPatterns: temporalPatterns,
                ClusteringRisk: clusteringRisk,
                IncidentPatterns: incidentPatterns,
                TenantReleasePatternScore: score,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                From: from,
                To: to,
                GeneratedAt: clock.UtcNow));
        }

        private static string HourBucket(int hour) =>
            hour < 6 ? "0-5"
            : hour < 12 ? "6-11"
            : hour < 18 ? "12-17"
            : "18-23";

        private static ReleaseClusteringTier ClassifyClusteringTier(double clusteringPerWeek, int threshold) =>
            clusteringPerWeek <= threshold ? ReleaseClusteringTier.Safe
            : clusteringPerWeek <= threshold * 5.0 / 3.0 ? ReleaseClusteringTier.Warning
            : clusteringPerWeek <= threshold * 5.0 / 3.0 * 2.0 ? ReleaseClusteringTier.Risky
            : ReleaseClusteringTier.Critical;

        private static decimal ComputeBatchSizeRisk(bool correlationSignificant, int largeCount, int total)
        {
            var penalty = correlationSignificant ? 15m : 0m;
            var largePct = total > 0 ? (decimal)largeCount / total : 0m;
            penalty += largePct > 0.5m ? 10m : largePct > 0.3m ? 5m : 0m;
            return Math.Min(25m, penalty);
        }

        private static decimal ComputeTemporalRisk(decimal highRiskDayPct, decimal endOfSprintPct)
        {
            var penalty = highRiskDayPct > 30m ? 15m : highRiskDayPct > 15m ? 8m : 0m;
            penalty += endOfSprintPct > 40m ? 10m : endOfSprintPct > 20m ? 5m : 0m;
            return Math.Min(25m, penalty);
        }

        private static decimal ComputeClusteringRisk(ReleaseClusteringTier tier) =>
            tier switch
            {
                ReleaseClusteringTier.Safe => 0m,
                ReleaseClusteringTier.Warning => 8m,
                ReleaseClusteringTier.Risky => 17m,
                ReleaseClusteringTier.Critical => 25m,
                _ => 0m
            };

        private static decimal ComputeIncidentRisk(decimal incidentHour1Rate)
        {
            if (incidentHour1Rate > 0.3m) return 25m;
            if (incidentHour1Rate > 0.15m) return 15m;
            if (incidentHour1Rate > 0.05m) return 8m;
            return 0m;
        }

        private static Dictionary<string, int> BuildEmptyHeatmapByHour() =>
            new() { ["0-5"] = 0, ["6-11"] = 0, ["12-17"] = 0, ["18-23"] = 0 };

        private static Dictionary<string, int> BuildEmptyHeatmapByDay() =>
            new()
            {
                ["Monday"] = 0, ["Tuesday"] = 0, ["Wednesday"] = 0,
                ["Thursday"] = 0, ["Friday"] = 0, ["Saturday"] = 0, ["Sunday"] = 0
            };
    }
}
