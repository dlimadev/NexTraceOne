using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetEnvironmentBehaviorComparisonReport;

/// <summary>
/// Feature: GetEnvironmentBehaviorComparisonReport — comparação de comportamento entre ambientes (Pre-Prod vs Prod).
///
/// <c>BehaviorSimilarityScore</c> = Performance 40% + Stability 35% + Configuration 25%
/// - Performance: semelhança de p99 latência entre ambientes
/// - Stability: semelhança de error rate e disponibilidade
/// - Configuration: ausência de config drift entre ambientes
///
/// <c>PromotionReadinessTier</c>: Ready / ConditionallyReady / NotReady / InsufficientData
/// - <c>BehaviorDivergenceAlerts</c>    — serviços com divergência significativa
/// - <c>CriticalServicesNotReady</c>    — serviços críticos com tier NotReady
/// - <c>HistoricalPromotionOutcome</c>  — correlação entre BehaviorSimilarityScore e sucesso de promoção
///
/// Wave BC.1 — Production Change Confidence (OperationalIntelligence Runtime).
/// </summary>
public static class GetEnvironmentBehaviorComparisonReport
{
    // ── Tier thresholds ────────────────────────────────────────────────────
    internal const decimal ReadyThreshold = 85m;
    internal const decimal ConditionallyReadyThreshold = 65m;
    internal const decimal NotReadyThreshold = 40m;

    // ── Score weights ──────────────────────────────────────────────────────
    internal const decimal PerformanceWeight = 0.40m;
    internal const decimal StabilityWeight = 0.35m;
    internal const decimal ConfigurationWeight = 0.25m;

    // ── Divergence thresholds ──────────────────────────────────────────────
    private const decimal LatencyDivergencePct = 20m;
    private const decimal ErrorRateDivergencePct = 5m;
    private const decimal MinServices = 3;

    internal const string DefaultSourceEnvironment = "pre-production";
    internal const string DefaultTargetEnvironment = "production";
    internal const int DefaultLookbackDays = 30;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        string SourceEnvironment = DefaultSourceEnvironment,
        string TargetEnvironment = DefaultTargetEnvironment,
        int LookbackDays = DefaultLookbackDays) : IQuery<Report>;

    // ── Validator ──────────────────────────────────────────────────────────
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(q => q.SourceEnvironment).NotEmpty().MaximumLength(100);
            RuleFor(q => q.TargetEnvironment).NotEmpty().MaximumLength(100);
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 90);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Tier de prontidão para promoção com base na similaridade de comportamento.</summary>
    public enum PromotionReadinessTier
    {
        /// <summary>Score ≥ 85% — ambiente pronto para promoção.</summary>
        Ready,
        /// <summary>Score ≥ 65% — pronto com condições, monitorização recomendada.</summary>
        ConditionallyReady,
        /// <summary>Score ≥ 40% — não está pronto, divergências significativas.</summary>
        NotReady,
        /// <summary>Dados insuficientes para avaliar prontidão.</summary>
        InsufficientData
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Alerta de divergência de comportamento entre ambientes.</summary>
    public sealed record BehaviorDivergenceAlert(
        string ServiceId,
        string ServiceName,
        string ServiceTier,
        string DivergenceType,
        decimal SourceValue,
        decimal TargetValue,
        decimal DivergencePct);

    /// <summary>Sumário de comportamento por serviço.</summary>
    public sealed record ServiceComparisonSummary(
        string ServiceId,
        string ServiceName,
        string ServiceTier,
        decimal BehaviorSimilarityScore,
        PromotionReadinessTier Tier,
        bool HasConfigDrift,
        int ConfigDriftKeyCount);

    /// <summary>Resultado histórico de promoções.</summary>
    public sealed record HistoricalPromotionOutcome(
        int TotalPromotions,
        int SuccessfulPromotions,
        decimal SuccessRatePct,
        decimal AvgSimilarityAtPromotion,
        bool HighSimilarityCorrelatesWithSuccess);

    /// <summary>Resultado do relatório de comparação de comportamento.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        string SourceEnvironment,
        string TargetEnvironment,
        int LookbackDays,
        decimal TenantBehaviorSimilarityScore,
        PromotionReadinessTier OverallTier,
        int CriticalServicesNotReadyCount,
        IReadOnlyList<BehaviorDivergenceAlert> BehaviorDivergenceAlerts,
        IReadOnlyList<ServiceComparisonSummary> ServiceComparisons,
        HistoricalPromotionOutcome? HistoricalOutcome);

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler(
        IEnvironmentBehaviorComparisonReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;

            var services = await reader.ListByTenantAsync(
                request.TenantId, request.SourceEnvironment, request.TargetEnvironment, cancellationToken);

            var outcomes = await reader.GetHistoricalPromotionOutcomesAsync(
                request.TenantId, request.LookbackDays, cancellationToken);

            if (services.Count < MinServices)
                return Result<Report>.Success(InsufficientDataReport(now, request));

            var comparisons = services
                .Select(s => new ServiceComparisonSummary(
                    ServiceId: s.ServiceId,
                    ServiceName: s.ServiceName,
                    ServiceTier: s.ServiceTier,
                    BehaviorSimilarityScore: ComputeServiceScore(s),
                    Tier: ClassifyServiceTier(ComputeServiceScore(s)),
                    HasConfigDrift: s.ConfigDriftDetected,
                    ConfigDriftKeyCount: s.ConfigDriftKeyCount))
                .ToList();

            var alerts = BuildAlerts(services);

            decimal tenantScore = comparisons.Count > 0
                ? Math.Round(comparisons.Average(c => c.BehaviorSimilarityScore), 1)
                : 0m;

            int criticalNotReady = comparisons.Count(c =>
                c.ServiceTier == "Critical"
                && c.Tier is PromotionReadinessTier.NotReady or PromotionReadinessTier.InsufficientData);

            var tier = ClassifyOverallTier(tenantScore, criticalNotReady);
            var historical = BuildHistoricalOutcome(outcomes);

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: request.TenantId,
                SourceEnvironment: request.SourceEnvironment,
                TargetEnvironment: request.TargetEnvironment,
                LookbackDays: request.LookbackDays,
                TenantBehaviorSimilarityScore: tenantScore,
                OverallTier: tier,
                CriticalServicesNotReadyCount: criticalNotReady,
                BehaviorDivergenceAlerts: alerts,
                ServiceComparisons: comparisons,
                HistoricalOutcome: historical));
        }

        private static decimal ComputeServiceScore(
            IEnvironmentBehaviorComparisonReader.ServiceBehaviorEntry s)
        {
            // Performance: similarity of p99 latency
            decimal perfScore = s.TargetP99Ms > 0
                ? Math.Max(0m, 100m - Math.Abs(s.SourceP99Ms - s.TargetP99Ms) / s.TargetP99Ms * 100m)
                : 100m;

            // Stability: error rate + availability
            decimal errorDiff = Math.Abs(s.SourceErrorRatePct - s.TargetErrorRatePct);
            decimal availDiff = Math.Abs(s.SourceAvailabilityPct - s.TargetAvailabilityPct);
            decimal stabilityScore = Math.Max(0m, 100m - errorDiff * 5m - availDiff * 2m);

            // Configuration
            decimal configScore = s.ConfigDriftDetected
                ? Math.Max(0m, 100m - s.ConfigDriftKeyCount * 10m)
                : 100m;

            return Math.Round(
                perfScore * PerformanceWeight
                + stabilityScore * StabilityWeight
                + configScore * ConfigurationWeight, 1);
        }

        private static PromotionReadinessTier ClassifyServiceTier(decimal score) => score switch
        {
            _ when score >= ReadyThreshold => PromotionReadinessTier.Ready,
            _ when score >= ConditionallyReadyThreshold => PromotionReadinessTier.ConditionallyReady,
            _ when score >= NotReadyThreshold => PromotionReadinessTier.NotReady,
            _ => PromotionReadinessTier.InsufficientData
        };

        private static PromotionReadinessTier ClassifyOverallTier(decimal score, int criticalNotReady)
        {
            if (criticalNotReady > 0) return PromotionReadinessTier.NotReady;
            return ClassifyServiceTier(score);
        }

        private static List<BehaviorDivergenceAlert> BuildAlerts(
            IReadOnlyList<IEnvironmentBehaviorComparisonReader.ServiceBehaviorEntry> services)
        {
            var alerts = new List<BehaviorDivergenceAlert>();
            foreach (var s in services)
            {
                if (s.TargetP99Ms > 0)
                {
                    decimal latDivPct = Math.Abs(s.SourceP99Ms - s.TargetP99Ms) / s.TargetP99Ms * 100m;
                    if (latDivPct > LatencyDivergencePct)
                        alerts.Add(new(s.ServiceId, s.ServiceName, s.ServiceTier,
                            "LatencyDivergence", s.SourceP99Ms, s.TargetP99Ms, Math.Round(latDivPct, 1)));
                }
                decimal errDiv = Math.Abs(s.SourceErrorRatePct - s.TargetErrorRatePct);
                if (errDiv > ErrorRateDivergencePct)
                    alerts.Add(new(s.ServiceId, s.ServiceName, s.ServiceTier,
                        "ErrorRateDivergence", s.SourceErrorRatePct, s.TargetErrorRatePct, Math.Round(errDiv, 1)));
            }
            return alerts;
        }

        private static HistoricalPromotionOutcome? BuildHistoricalOutcome(
            IReadOnlyList<IEnvironmentBehaviorComparisonReader.PromotionOutcomeEntry> outcomes)
        {
            if (outcomes.Count == 0) return null;
            int success = outcomes.Count(o => o.SuccessfulPromotion);
            decimal successRate = Math.Round((decimal)success / outcomes.Count * 100m, 1);
            decimal avgSimilarity = Math.Round(outcomes.Average(o => o.SimilarityScoreAtPromotion), 1);
            bool correlates = outcomes
                .Where(o => o.SimilarityScoreAtPromotion >= ReadyThreshold)
                .Count(o => o.SuccessfulPromotion) >
                outcomes.Where(o => o.SimilarityScoreAtPromotion >= ReadyThreshold).Count() * 0.7m;
            return new(outcomes.Count, success, successRate, avgSimilarity, correlates);
        }

        private static Report InsufficientDataReport(DateTimeOffset now, Query request)
            => new(
                GeneratedAt: now,
                TenantId: request.TenantId,
                SourceEnvironment: request.SourceEnvironment,
                TargetEnvironment: request.TargetEnvironment,
                LookbackDays: request.LookbackDays,
                TenantBehaviorSimilarityScore: 0m,
                OverallTier: PromotionReadinessTier.InsufficientData,
                CriticalServicesNotReadyCount: 0,
                BehaviorDivergenceAlerts: [],
                ServiceComparisons: [],
                HistoricalOutcome: null);
    }
}
