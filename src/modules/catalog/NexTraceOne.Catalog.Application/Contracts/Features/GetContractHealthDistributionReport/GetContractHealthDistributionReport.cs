using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractHealthDistributionReport;

/// <summary>
/// Feature: GetContractHealthDistributionReport — distribuição de scores de saúde de contratos.
///
/// Agrega todos os scores de saúde de contratos do tenant e produz:
/// - contagem por banda de saúde (Healthy/Fair/AtRisk/Critical)
/// - lista dos contratos mais críticos (score mais baixo)
/// - scores médios por dimensão (breaking change, consumer impact, review recency, examples, policy, docs)
/// - percentagem de contratos saudáveis e críticos
///
/// Serve como fonte de verdade da qualidade dos contratos registados no NexTraceOne.
/// Orientado para Architect, Tech Lead e Platform Admin personas.
///
/// Wave M.1 — Contract Health Distribution Report (Catalog Contracts).
/// </summary>
public static class GetContractHealthDistributionReport
{
    /// <summary>
    /// <para><c>TopCriticalCount</c>: número máximo de contratos críticos a listar (1–50, default 10).</para>
    /// <para><c>HealthyThreshold</c>: score mínimo para Healthy (50–100, default 80).</para>
    /// <para><c>FairThreshold</c>: score mínimo para Fair (20–79, default 60).</para>
    /// <para><c>AtRiskThreshold</c>: score mínimo para AtRisk (1–59, default 40).</para>
    /// </summary>
    public sealed record Query(
        int TopCriticalCount = 10,
        int HealthyThreshold = 80,
        int FairThreshold = 60,
        int AtRiskThreshold = 40) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Banda de saúde do contrato com base no score agregado.</summary>
    public enum ContractHealthBand
    {
        /// <summary>Score ≥ HealthyThreshold — contrato bem governado.</summary>
        Healthy,
        /// <summary>Score ≥ FairThreshold — contrato aceitável mas com gaps.</summary>
        Fair,
        /// <summary>Score ≥ AtRiskThreshold — contrato com problemas relevantes.</summary>
        AtRisk,
        /// <summary>Score < AtRiskThreshold — contrato crítico que precisa de atenção urgente.</summary>
        Critical
    }

    /// <summary>Sumário de um contrato com saúde crítica.</summary>
    public sealed record CriticalContractSummary(
        Guid ApiAssetId,
        int OverallScore,
        ContractHealthBand Band,
        int BreakingChangeFrequencyScore,
        int ConsumerImpactScore,
        int ReviewRecencyScore,
        int ExampleCoverageScore,
        int PolicyComplianceScore,
        int DocumentationScore);

    /// <summary>Scores médios por dimensão de saúde.</summary>
    public sealed record DimensionAverages(
        decimal AvgBreakingChangeFrequency,
        decimal AvgConsumerImpact,
        decimal AvgReviewRecency,
        decimal AvgExampleCoverage,
        decimal AvgPolicyCompliance,
        decimal AvgDocumentation,
        decimal AvgOverall);

    /// <summary>Resultado do relatório de distribuição de saúde de contratos.</summary>
    public sealed record Report(
        int TotalContracts,
        int HealthyCount,
        int FairCount,
        int AtRiskCount,
        int CriticalCount,
        decimal HealthyPercent,
        decimal CriticalPercent,
        DimensionAverages DimensionAverages,
        IReadOnlyList<CriticalContractSummary> TopCriticalContracts,
        int HealthyThreshold,
        int FairThreshold,
        int AtRiskThreshold,
        DateTimeOffset GeneratedAt);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TopCriticalCount).InclusiveBetween(1, 50);
            RuleFor(q => q.HealthyThreshold).InclusiveBetween(50, 100);
            RuleFor(q => q.FairThreshold).InclusiveBetween(20, 79);
            RuleFor(q => q.AtRiskThreshold).InclusiveBetween(1, 59);
            RuleFor(q => q)
                .Must(q => q.HealthyThreshold > q.FairThreshold && q.FairThreshold > q.AtRiskThreshold)
                .WithMessage("HealthyThreshold > FairThreshold > AtRiskThreshold must hold.");
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IContractHealthScoreRepository contractHealthScoreRepository,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);

            // Retrieve all scores (threshold=101 returns all since OverallScore is 0-100)
            var allScores = await contractHealthScoreRepository
                .ListBelowThresholdAsync(101, cancellationToken);

            if (allScores.Count == 0)
            {
                var emptyDims = new DimensionAverages(0, 0, 0, 0, 0, 0, 0);
                return Result<Report>.Success(new Report(
                    TotalContracts: 0,
                    HealthyCount: 0,
                    FairCount: 0,
                    AtRiskCount: 0,
                    CriticalCount: 0,
                    HealthyPercent: 0m,
                    CriticalPercent: 0m,
                    DimensionAverages: emptyDims,
                    TopCriticalContracts: [],
                    HealthyThreshold: query.HealthyThreshold,
                    FairThreshold: query.FairThreshold,
                    AtRiskThreshold: query.AtRiskThreshold,
                    GeneratedAt: clock.UtcNow));
            }

            // Band classification
            var healthy = allScores.Where(s => s.OverallScore >= query.HealthyThreshold).ToList();
            var fair = allScores.Where(s => s.OverallScore >= query.FairThreshold && s.OverallScore < query.HealthyThreshold).ToList();
            var atRisk = allScores.Where(s => s.OverallScore >= query.AtRiskThreshold && s.OverallScore < query.FairThreshold).ToList();
            var critical = allScores.Where(s => s.OverallScore < query.AtRiskThreshold).ToList();

            var total = allScores.Count;
            var healthyPct = total == 0 ? 0m : Math.Round((decimal)healthy.Count / total * 100m, 1);
            var criticalPct = total == 0 ? 0m : Math.Round((decimal)critical.Count / total * 100m, 1);

            // Dimension averages
            var dims = new DimensionAverages(
                AvgBreakingChangeFrequency: Math.Round(allScores.Average(s => (decimal)s.BreakingChangeFrequencyScore), 1),
                AvgConsumerImpact: Math.Round(allScores.Average(s => (decimal)s.ConsumerImpactScore), 1),
                AvgReviewRecency: Math.Round(allScores.Average(s => (decimal)s.ReviewRecencyScore), 1),
                AvgExampleCoverage: Math.Round(allScores.Average(s => (decimal)s.ExampleCoverageScore), 1),
                AvgPolicyCompliance: Math.Round(allScores.Average(s => (decimal)s.PolicyComplianceScore), 1),
                AvgDocumentation: Math.Round(allScores.Average(s => (decimal)s.DocumentationScore), 1),
                AvgOverall: Math.Round(allScores.Average(s => (decimal)s.OverallScore), 1));

            // Top critical contracts sorted by score ascending
            var topCritical = allScores
                .OrderBy(s => s.OverallScore)
                .Take(query.TopCriticalCount)
                .Select(s => new CriticalContractSummary(
                    ApiAssetId: s.ApiAssetId,
                    OverallScore: s.OverallScore,
                    Band: ClassifyBand(s.OverallScore, query.HealthyThreshold, query.FairThreshold, query.AtRiskThreshold),
                    BreakingChangeFrequencyScore: s.BreakingChangeFrequencyScore,
                    ConsumerImpactScore: s.ConsumerImpactScore,
                    ReviewRecencyScore: s.ReviewRecencyScore,
                    ExampleCoverageScore: s.ExampleCoverageScore,
                    PolicyComplianceScore: s.PolicyComplianceScore,
                    DocumentationScore: s.DocumentationScore))
                .ToList();

            return Result<Report>.Success(new Report(
                TotalContracts: total,
                HealthyCount: healthy.Count,
                FairCount: fair.Count,
                AtRiskCount: atRisk.Count,
                CriticalCount: critical.Count,
                HealthyPercent: healthyPct,
                CriticalPercent: criticalPct,
                DimensionAverages: dims,
                TopCriticalContracts: topCritical,
                HealthyThreshold: query.HealthyThreshold,
                FairThreshold: query.FairThreshold,
                AtRiskThreshold: query.AtRiskThreshold,
                GeneratedAt: clock.UtcNow));
        }

        private static ContractHealthBand ClassifyBand(int score, int healthy, int fair, int atRisk)
            => score >= healthy ? ContractHealthBand.Healthy
             : score >= fair ? ContractHealthBand.Fair
             : score >= atRisk ? ContractHealthBand.AtRisk
             : ContractHealthBand.Critical;
    }
}
