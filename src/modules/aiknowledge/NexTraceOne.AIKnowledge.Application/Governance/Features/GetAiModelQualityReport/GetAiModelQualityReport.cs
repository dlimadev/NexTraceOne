using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiModelQualityReport;

/// <summary>
/// Feature: GetAiModelQualityReport — qualidade de modelos de IA em produção.
///
/// Agrega métricas de performance de modelo (quando feedback disponível), latência de
/// inferência, taxa de fallback e comparação com período anterior para fornecer visão
/// executiva da qualidade de IA em uso no tenant.
///
/// ModelQualityTier:
/// - <c>Excellent</c> — Accuracy ≥95% + LowConf ≤5% + LatencyP95 ≤ latency_budget_ms
/// - <c>Good</c> — Accuracy ≥80% + LowConf ≤15% + LatencyP95 ≤ 2× budget
/// - <c>Degraded</c> — Accuracy ≥60% ou LowConf ≤30%
/// - <c>Poor</c> — abaixo de Degraded ou sem dados suficientes
///
/// Wave AT.2 — AI Model Quality &amp; Drift Governance (AIKnowledge Governance).
/// </summary>
public static class GetAiModelQualityReport
{
    // ── Configuration keys ─────────────────────────────────────────────────
    internal const string MinSamplesKey = "ai.model_quality.min_samples_for_quality";
    internal const string LowConfThresholdKey = "ai.model_quality.low_confidence_threshold";
    internal const string LatencyBudgetKey = "ai.model_quality.latency_budget_ms";
    internal const int DefaultMinSamples = 100;
    internal const double DefaultLowConfThreshold = 0.6;
    internal const int DefaultLatencyBudgetMs = 500;

    // ── Tier thresholds ────────────────────────────────────────────────────
    private const double ExcellentAccuracy = 95.0;
    private const double GoodAccuracy = 80.0;
    private const double DegradedAccuracy = 60.0;
    private const double ExcellentLowConf = 5.0;
    private const double GoodLowConf = 15.0;
    private const double DegradedLowConf = 30.0;
    private const int DefaultLookbackDays = 30;
    private const int DefaultTrendDays = 7;

    // ── Query ──────────────────────────────────────────────────────────────
    /// <summary>Query para o relatório de qualidade de modelos de IA.</summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays) : IQuery<Report>;

    /// <summary>Validador da query <see cref="Query"/>.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LookbackDays).InclusiveBetween(7, 90);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    /// <summary>Tier de qualidade de modelo de IA.</summary>
    public enum ModelQualityTier { Excellent, Good, Degraded, Poor }

    /// <summary>Tendência de qualidade vs. período anterior.</summary>
    public enum QualityTrend { Improving, Stable, Degrading }

    // ── Value objects ──────────────────────────────────────────────────────
    /// <summary>Linha de qualidade por modelo.</summary>
    public sealed record ModelQualityRow(
        Guid ModelId,
        string ModelName,
        string ServiceId,
        int SampleCount,
        double? AccuracyRate,
        double FeedbackCoverageRate,
        double AvgConfidenceScore,
        double LowConfidencePredictionRate,
        double? InferenceLatencyP50Ms,
        double? InferenceLatencyP95Ms,
        double FallbackRate,
        QualityTrend Trend,
        ModelQualityTier Tier);

    /// <summary>Anomalia de qualidade — modelo com degradação activa.</summary>
    public sealed record QualityAnomaly(
        Guid ModelId,
        string ModelName,
        QualityTrend Trend,
        string AnomalyDescription);

    /// <summary>Sumário global de qualidade de IA do tenant.</summary>
    public sealed record TenantAiQualitySummary(
        int TotalModels,
        int ModelsWithFeedback,
        double TenantAiQualityScore,
        int LowConfidenceModelCount,
        int ExcellentCount,
        int GoodCount,
        int DegradedCount,
        int PoorCount);

    /// <summary>Relatório completo de qualidade de modelos de IA.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<ModelQualityRow> ByModel,
        TenantAiQualitySummary Summary,
        IReadOnlyList<QualityAnomaly> QualityAnomalies,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler da query <see cref="Query"/>.</summary>
    public sealed class Handler(
        IAiModelQualityReader qualityReader,
        IConfigurationResolutionService configService,
        NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;

            // Resolve config
            var minSamplesCfg = await configService.ResolveEffectiveValueAsync(
                MinSamplesKey, ConfigurationScope.System, null, cancellationToken);
            var lowConfCfg = await configService.ResolveEffectiveValueAsync(
                LowConfThresholdKey, ConfigurationScope.System, null, cancellationToken);
            var latencyBudgetCfg = await configService.ResolveEffectiveValueAsync(
                LatencyBudgetKey, ConfigurationScope.System, null, cancellationToken);

            var minSamples = int.TryParse(minSamplesCfg?.EffectiveValue, out var ms) ? ms : DefaultMinSamples;
            var latencyBudget = int.TryParse(latencyBudgetCfg?.EffectiveValue, out var lb) ? lb : DefaultLatencyBudgetMs;
            var lowConfThreshold = double.TryParse(lowConfCfg?.EffectiveValue, out var lc) ? lc : DefaultLowConfThreshold;

            var from = now.AddDays(-request.LookbackDays);
            var rows = await qualityReader.GetQualityRowsAsync(
                request.TenantId, from, now, minSamples, cancellationToken);

            // Classify tiers
            var classifiedRows = rows
                .Select(r => r with { Tier = ClassifyTier(r, latencyBudget, lowConfThreshold) })
                .ToList();

            // Tenant quality score — weighted average by tier (Critical×3, Standard×2)
            var totalWeight = classifiedRows.Count > 0 ? classifiedRows.Count : 1;
            var tierScoreSum = classifiedRows.Sum(r => TierToScore(r.Tier));
            var tenantScore = totalWeight > 0
                ? Math.Round(tierScoreSum / (classifiedRows.Count * 100.0) * 100.0, 1)
                : 0.0;

            var modelsWithFeedback = classifiedRows.Count(r => r.FeedbackCoverageRate > 0);
            var lowConfCount = classifiedRows.Count(r =>
                r.AvgConfidenceScore <= lowConfThreshold);

            var summary = new TenantAiQualitySummary(
                TotalModels: classifiedRows.Count,
                ModelsWithFeedback: modelsWithFeedback,
                TenantAiQualityScore: tenantScore,
                LowConfidenceModelCount: lowConfCount,
                ExcellentCount: classifiedRows.Count(r => r.Tier == ModelQualityTier.Excellent),
                GoodCount: classifiedRows.Count(r => r.Tier == ModelQualityTier.Good),
                DegradedCount: classifiedRows.Count(r => r.Tier == ModelQualityTier.Degraded),
                PoorCount: classifiedRows.Count(r => r.Tier == ModelQualityTier.Poor));

            // Quality anomalies — models with Degrading trend and not already Poor
            var anomalies = classifiedRows
                .Where(r => r.Trend == QualityTrend.Degrading)
                .Select(r => new QualityAnomaly(
                    r.ModelId,
                    r.ModelName,
                    r.Trend,
                    BuildAnomalyDescription(r)))
                .ToList();

            return Result<Report>.Success(new Report(
                TenantId: request.TenantId,
                ByModel: classifiedRows,
                Summary: summary,
                QualityAnomalies: anomalies,
                GeneratedAt: now));
        }

        private static ModelQualityTier ClassifyTier(
            ModelQualityRow row, int latencyBudgetMs, double lowConfThreshold)
        {
            var lowConfPct = row.LowConfidencePredictionRate;
            var latencyOk = row.InferenceLatencyP95Ms is null || row.InferenceLatencyP95Ms <= latencyBudgetMs;

            // Excellent: accuracy ≥95% (if feedback) + low conf ≤5% + latency within budget
            if (row.AccuracyRate >= ExcellentAccuracy
                && lowConfPct <= ExcellentLowConf
                && latencyOk)
                return ModelQualityTier.Excellent;

            // Good: accuracy ≥80% + low conf ≤15% + latency within 2× budget
            var latencyGoodOk = row.InferenceLatencyP95Ms is null || row.InferenceLatencyP95Ms <= latencyBudgetMs * 2.0;
            if ((row.AccuracyRate is null || row.AccuracyRate >= GoodAccuracy)
                && lowConfPct <= GoodLowConf
                && latencyGoodOk)
                return ModelQualityTier.Good;

            // Degraded: accuracy ≥60% or low conf ≤30%
            if ((row.AccuracyRate is null || row.AccuracyRate >= DegradedAccuracy)
                && lowConfPct <= DegradedLowConf)
                return ModelQualityTier.Degraded;

            return ModelQualityTier.Poor;
        }

        private static double TierToScore(ModelQualityTier tier) => tier switch
        {
            ModelQualityTier.Excellent => 100.0,
            ModelQualityTier.Good => 75.0,
            ModelQualityTier.Degraded => 40.0,
            ModelQualityTier.Poor => 10.0,
            _ => 0.0
        };

        private static string BuildAnomalyDescription(ModelQualityRow row)
        {
            var parts = new List<string>();
            if (row.AccuracyRate < DegradedAccuracy)
                parts.Add($"accuracy degraded to {row.AccuracyRate:F1}%");
            if (row.LowConfidencePredictionRate > DegradedLowConf)
                parts.Add($"low confidence rate at {row.LowConfidencePredictionRate:F1}%");
            if (row.Trend == QualityTrend.Degrading)
                parts.Add("trend is degrading vs. prior 7d");
            return parts.Count > 0 ? string.Join("; ", parts) : "quality degrading trend detected";
        }
    }
}
