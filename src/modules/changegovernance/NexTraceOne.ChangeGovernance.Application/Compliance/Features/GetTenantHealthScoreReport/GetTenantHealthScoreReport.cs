using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetTenantHealthScoreReport;

/// <summary>
/// Feature: GetTenantHealthScoreReport — scorecard de saúde global do tenant por dimensão operacional.
///
/// Calcula <c>TenantHealthScore</c> (0–100) por 6 pilares ponderados:
/// - Service Governance     (20%) — % serviços com ownership + tier + contratos definidos
/// - Change Confidence      (20%) — média de ConfidenceScore das últimas N releases
/// - Operational Reliability(20%) — SLO compliance rate + MTTR DORA tier
/// - Contract Health        (15%) — % contratos Approved sem breaking changes não comunicados
/// - Compliance Coverage    (15%) — % serviços avaliados em ≥ 2 standards
/// - FinOps Efficiency      (10%) — ausência de serviços WasteAlert e WasteTier tenant
///
/// Classifica por <c>HealthTier</c>:
/// - <c>Excellent</c> — score ≥ 85
/// - <c>Good</c>      — score ≥ 65
/// - <c>Fair</c>      — score ≥ 40
/// - <c>AtRisk</c>    — score &lt; 40
///
/// Inclui:
/// - <c>PillarBreakdown</c>     — score e contribuição por pilar
/// - <c>TrendComparison</c>     — score do período actual vs. período anterior
/// - <c>TopIssues</c>           — top 5 issues mais impactantes
/// - <c>ActionableItems</c>     — ações concretas para subir de tier
///
/// Wave AJ.2 — Multi-Tenant Governance Intelligence (ChangeGovernance Compliance).
/// </summary>
public static class GetTenantHealthScoreReport
{
    // ── Pillar weights (sum = 100) ─────────────────────────────────────────
    private const decimal ServiceGovernanceWeight = 0.20m;
    private const decimal ChangeConfidenceWeight = 0.20m;
    private const decimal OperationalReliabilityWeight = 0.20m;
    private const decimal ContractHealthWeight = 0.15m;
    private const decimal ComplianceCoverageWeight = 0.15m;
    private const decimal FinOpsEfficiencyWeight = 0.10m;

    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal ExcellentThreshold = 85m;
    private const decimal GoodThreshold = 65m;
    private const decimal FairThreshold = 40m;

    private const int TopIssuesCount = 5;
    internal const int DefaultLookbackDays = 30;

    // ── Query ──────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: período de análise em dias (7–90, default 30).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Tier de saúde global do tenant.</summary>
    public enum HealthTier
    {
        /// <summary>Score &lt; 40 — saúde em risco, acção imediata recomendada.</summary>
        AtRisk,
        /// <summary>Score ≥ 40 — saúde razoável, melhorias importantes necessárias.</summary>
        Fair,
        /// <summary>Score ≥ 65 — boa saúde operacional, margem de melhoria moderada.</summary>
        Good,
        /// <summary>Score ≥ 85 — saúde excelente, referência de boas práticas.</summary>
        Excellent
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Score e contribuição de um pilar para o TenantHealthScore.</summary>
    public sealed record PillarScore(
        string PillarName,
        decimal Score,
        decimal Weight,
        decimal WeightedContribution);

    /// <summary>Comparação de score entre o período actual e o anterior.</summary>
    public sealed record TrendComparison(
        decimal CurrentScore,
        decimal PreviousScore,
        decimal Delta,
        string Trend);

    /// <summary>Issue que mais impacta o score global.</summary>
    public sealed record HealthIssue(
        string PillarName,
        string Description,
        decimal EstimatedScoreImpact);

    /// <summary>Resultado do scorecard de saúde do tenant.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        decimal TenantHealthScore,
        HealthTier Tier,
        IReadOnlyList<PillarScore> PillarBreakdown,
        TrendComparison Trend,
        IReadOnlyList<HealthIssue> TopIssues,
        IReadOnlyList<string> ActionableItems);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 90);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly ITenantHealthDataReader _healthReader;
        private readonly IDateTimeProvider _clock;

        public Handler(
            ITenantHealthDataReader healthReader,
            IDateTimeProvider clock)
        {
            _healthReader = Guard.Against.Null(healthReader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var since = now.AddDays(-query.LookbackDays);

            // 1. Get current period pillar data
            var current = await _healthReader.GetPillarDataAsync(
                query.TenantId, since, now, cancellationToken);

            // 2. Get previous period pillar data for trend comparison
            var prevUntil = since;
            var prevSince = prevUntil.AddDays(-query.LookbackDays);
            var previous = await _healthReader.GetPillarDataAsync(
                query.TenantId, prevSince, prevUntil, cancellationToken);

            // 3. Compute weighted health score
            var pillars = BuildPillarBreakdown(current);
            decimal healthScore = Math.Round(Math.Clamp(pillars.Sum(p => p.WeightedContribution), 0m, 100m), 1);
            var tier = ClassifyTier(healthScore);

            // 4. Previous score for trend
            var prevPillars = BuildPillarBreakdown(previous);
            decimal prevScore = Math.Round(Math.Clamp(prevPillars.Sum(p => p.WeightedContribution), 0m, 100m), 1);
            decimal delta = Math.Round(healthScore - prevScore, 1);

            var trend = new TrendComparison(
                CurrentScore: healthScore,
                PreviousScore: prevScore,
                Delta: delta,
                Trend: delta > 2m ? "Improving" : delta < -2m ? "Declining" : "Stable");

            // 5. Identify top issues (lowest-contributing pillars)
            var topIssues = pillars
                .OrderBy(p => p.Score)
                .Take(TopIssuesCount)
                .Select(p => new HealthIssue(
                    PillarName: p.PillarName,
                    Description: BuildIssueDescription(p.PillarName, p.Score),
                    EstimatedScoreImpact: Math.Round((100m - p.Score) * p.Weight, 1)))
                .OrderByDescending(i => i.EstimatedScoreImpact)
                .ToList();

            // 6. Generate actionable items from top issues
            var actions = topIssues
                .Take(3)
                .Select(i => BuildAction(i.PillarName, i.EstimatedScoreImpact))
                .Distinct()
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                TenantHealthScore: healthScore,
                Tier: tier,
                PillarBreakdown: pillars,
                Trend: trend,
                TopIssues: topIssues,
                ActionableItems: actions));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static IReadOnlyList<PillarScore> BuildPillarBreakdown(
            ITenantHealthDataReader.TenantHealthPillarData data)
        {
            return
            [
                MakePillar("ServiceGovernance", data.ServiceGovernanceScore, ServiceGovernanceWeight),
                MakePillar("ChangeConfidence", data.ChangeConfidenceScore, ChangeConfidenceWeight),
                MakePillar("OperationalReliability", data.OperationalReliabilityScore, OperationalReliabilityWeight),
                MakePillar("ContractHealth", data.ContractHealthScore, ContractHealthWeight),
                MakePillar("ComplianceCoverage", data.ComplianceCoverageScore, ComplianceCoverageWeight),
                MakePillar("FinOpsEfficiency", data.FinOpsEfficiencyScore, FinOpsEfficiencyWeight)
            ];
        }

        private static PillarScore MakePillar(string name, decimal score, decimal weight)
        {
            decimal clamped = Math.Clamp(score, 0m, 100m);
            return new PillarScore(name, Math.Round(clamped, 1), weight, Math.Round(clamped * weight, 1));
        }

        internal static HealthTier ClassifyTier(decimal score) => score switch
        {
            >= ExcellentThreshold => HealthTier.Excellent,
            >= GoodThreshold => HealthTier.Good,
            >= FairThreshold => HealthTier.Fair,
            _ => HealthTier.AtRisk
        };

        private static string BuildIssueDescription(string pillarName, decimal score) => pillarName switch
        {
            "ServiceGovernance" => $"Service governance score at {score:F0}% — services may lack ownership, tier or contract definitions",
            "ChangeConfidence" => $"Change confidence at {score:F0}% — low confidence scores in recent releases indicate deployment risk",
            "OperationalReliability" => $"Operational reliability at {score:F0}% — SLO breaches or elevated MTTR detected",
            "ContractHealth" => $"Contract health at {score:F0}% — unapproved or contracts with unaddressed breaking changes detected",
            "ComplianceCoverage" => $"Compliance coverage at {score:F0}% — services missing evaluations against required standards",
            "FinOpsEfficiency" => $"FinOps efficiency at {score:F0}% — waste alerts or overprovisioning detected",
            _ => $"{pillarName} score at {score:F0}%"
        };

        private static string BuildAction(string pillarName, decimal impact) => pillarName switch
        {
            "ServiceGovernance" => "Assign ownership and define service tier for all services without governance metadata",
            "ChangeConfidence" => "Complete evidence packs and run validation gates for pending releases to improve confidence",
            "OperationalReliability" => "Review and address active SLO violations and open incident MTTR root causes",
            "ContractHealth" => "Review and approve pending contracts; communicate breaking changes to downstream consumers",
            "ComplianceCoverage" => "Run compliance evaluations for services missing assessment in key regulatory standards",
            "FinOpsEfficiency" => "Identify and eliminate idle or overprovisioned resources flagged in the FinOps waste report",
            _ => $"Improve {pillarName} to gain approximately {impact:F0} points in the health score"
        };
    }
}
