using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetMultiDimensionalPromotionConfidenceReport;

/// <summary>
/// Feature: GetMultiDimensionalPromotionConfidenceReport — score de confiança de promoção 8-dimensional.
///
/// Dimensões (peso igual: 12.5% cada):
/// 1. BlastRadius          — risco de impacto (invertido: score alto = blast radius pequeno)
/// 2. Rollback             — viabilidade de rollback
/// 3. EnvBehavior          — similaridade de comportamento entre ambientes
/// 4. EvidenceIntegrity    — integridade dos Evidence Packs
/// 5. ContractCompliance   — conformidade do contrato com consumidores
/// 6. SloHealth            — saúde dos SLOs (error budget disponível)
/// 7. ChaosResilience      — resiliência comprovada por testes de chaos
/// 8. ChangePattern        — padrão de mudança (clustering/temporal risk)
///
/// <c>PromotionConfidenceTier</c>: HighConfidence / MediumConfidence / LowConfidence / BlockingIssues
/// <c>PromotionRecommendation</c>: ProceedAutomatically / ProceedWithConditions / RequireManualApproval / Block
/// - <c>BlockingFactors</c>             — dimensões abaixo do threshold de bloqueio
/// - <c>HistoricalConfidenceVsOutcome</c> — correlação entre score e resultado real
///
/// Endpoint: GET /changes/releases/{id}/promotion-confidence
/// Wave BC.3 — Production Change Confidence (ChangeGovernance/OI).
/// </summary>
public static class GetMultiDimensionalPromotionConfidenceReport
{
    // ── Tier thresholds ────────────────────────────────────────────────────
    internal const decimal HighConfidenceThreshold = 80m;
    internal const decimal MediumConfidenceThreshold = 60m;
    internal const decimal LowConfidenceThreshold = 40m;

    // ── Blocking threshold ─────────────────────────────────────────────────
    internal const decimal BlockingDimensionThreshold = 30m;

    internal const int DefaultLookbackDays = 90;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        string ReleaseId,
        int LookbackDays = DefaultLookbackDays) : IQuery<Report>;

    // ── Validator ──────────────────────────────────────────────────────────
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(q => q.ReleaseId).NotEmpty().MaximumLength(200);
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 365);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Tier de confiança para promoção.</summary>
    public enum PromotionConfidenceTier
    {
        /// <summary>Score ≥ 80% sem blocking factors — alta confiança.</summary>
        HighConfidence,
        /// <summary>Score ≥ 60% — confiança média, condições recomendadas.</summary>
        MediumConfidence,
        /// <summary>Score ≥ 40% — baixa confiança, aprovação manual necessária.</summary>
        LowConfidence,
        /// <summary>Score &lt; 40% ou blocking factors — promoção deve ser bloqueada.</summary>
        BlockingIssues
    }

    /// <summary>Recomendação de acção para a promoção.</summary>
    public enum PromotionRecommendation
    {
        /// <summary>Score ≥ 80% sem blocking factors — prosseguir automaticamente.</summary>
        ProceedAutomatically,
        /// <summary>Score ≥ 60% — prosseguir com condições de monitorização.</summary>
        ProceedWithConditions,
        /// <summary>Score ≥ 40% — requerer aprovação manual antes de promover.</summary>
        RequireManualApproval,
        /// <summary>Score &lt; 40% ou blocking factors — bloquear promoção.</summary>
        Block
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Score por dimensão de confiança.</summary>
    public sealed record DimensionScore(
        string DimensionName,
        decimal Score,
        bool IsBlocking,
        string? Evidence);

    /// <summary>Factor de bloqueio identificado.</summary>
    public sealed record BlockingFactor(
        string DimensionName,
        decimal Score,
        string BlockingReason);

    /// <summary>Correlação histórica entre score e resultado de promoção.</summary>
    public sealed record HistoricalConfidenceOutcome(
        int TotalPromotions,
        int SuccessfulPromotions,
        decimal SuccessRatePct,
        decimal AvgConfidenceAtPromotion,
        bool HighConfidenceCorrelatesWithSuccess);

    /// <summary>Resultado do relatório de confiança multi-dimensional.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        string ReleaseId,
        decimal OverallConfidenceScore,
        PromotionConfidenceTier Tier,
        PromotionRecommendation Recommendation,
        IReadOnlyList<DimensionScore> Dimensions,
        IReadOnlyList<BlockingFactor> BlockingFactors,
        HistoricalConfidenceOutcome? HistoricalOutcome);

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler(
        IMultiDimensionalPromotionConfidenceReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        private static readonly string[] DimensionNames =
        [
            "BlastRadius", "Rollback", "EnvBehavior", "EvidenceIntegrity",
            "ContractCompliance", "SloHealth", "ChaosResilience", "ChangePattern"
        ];

        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;

            var data = await reader.GetByReleaseAsync(request.TenantId, request.ReleaseId, cancellationToken);
            var outcomes = await reader.GetHistoricalOutcomesAsync(request.TenantId, request.LookbackDays, cancellationToken);

            var rawScores = new decimal[]
            {
                data.BlastRadiusScore,
                data.RollbackScore,
                data.EnvBehaviorScore,
                data.EvidenceIntegrityScore,
                data.ContractComplianceScore,
                data.SloHealthScore,
                data.ChaosResilienceScore,
                data.ChangePatternScore
            };

            var dimensions = DimensionNames
                .Zip(rawScores, (name, score) => new DimensionScore(
                    DimensionName: name,
                    Score: Math.Round(score, 1),
                    IsBlocking: score < BlockingDimensionThreshold,
                    Evidence: data.MissingDimensions.Contains(name) ? "No data available" : null))
                .ToList();

            var blocking = dimensions
                .Where(d => d.IsBlocking)
                .Select(d => new BlockingFactor(
                    DimensionName: d.DimensionName,
                    Score: d.Score,
                    BlockingReason: $"{d.DimensionName} score {d.Score:F1}% is below blocking threshold {BlockingDimensionThreshold}%"))
                .ToList();

            decimal overallScore = dimensions.Count > 0
                ? Math.Round(dimensions.Average(d => d.Score), 1)
                : 0m;

            var tier = ClassifyTier(overallScore, blocking.Count > 0);
            var recommendation = ClassifyRecommendation(tier);
            var historical = BuildHistorical(outcomes);

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: request.TenantId,
                ReleaseId: request.ReleaseId,
                OverallConfidenceScore: overallScore,
                Tier: tier,
                Recommendation: recommendation,
                Dimensions: dimensions,
                BlockingFactors: blocking,
                HistoricalOutcome: historical));
        }

        private static PromotionConfidenceTier ClassifyTier(decimal score, bool hasBlocking)
        {
            if (hasBlocking || score < LowConfidenceThreshold) return PromotionConfidenceTier.BlockingIssues;
            if (score >= HighConfidenceThreshold) return PromotionConfidenceTier.HighConfidence;
            if (score >= MediumConfidenceThreshold) return PromotionConfidenceTier.MediumConfidence;
            return PromotionConfidenceTier.LowConfidence;
        }

        private static PromotionRecommendation ClassifyRecommendation(PromotionConfidenceTier tier) => tier switch
        {
            PromotionConfidenceTier.HighConfidence => PromotionRecommendation.ProceedAutomatically,
            PromotionConfidenceTier.MediumConfidence => PromotionRecommendation.ProceedWithConditions,
            PromotionConfidenceTier.LowConfidence => PromotionRecommendation.RequireManualApproval,
            _ => PromotionRecommendation.Block
        };

        private static HistoricalConfidenceOutcome? BuildHistorical(
            IReadOnlyList<IMultiDimensionalPromotionConfidenceReader.HistoricalOutcomeEntry> outcomes)
        {
            if (outcomes.Count == 0) return null;
            int success = outcomes.Count(o => o.SuccessfulPromotion);
            decimal rate = Math.Round((decimal)success / outcomes.Count * 100m, 1);
            decimal avg = Math.Round(outcomes.Average(o => o.ConfidenceScoreAtPromotion), 1);
            var highConf = outcomes.Where(o => o.ConfidenceScoreAtPromotion >= HighConfidenceThreshold).ToList();
            bool correlates = highConf.Count > 0
                && (decimal)highConf.Count(o => o.SuccessfulPromotion) / highConf.Count >= 0.7m;
            return new(outcomes.Count, success, rate, avg, correlates);
        }
    }
}
