using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetPeerReviewCoverageReport;

/// <summary>
/// Feature: GetPeerReviewCoverageReport — cobertura de peer review em mudanças e contratos.
///
/// Calcula métricas de cobertura de revisão entre pares:
/// - <c>ReviewCoverageRate</c>             — % de mudanças com peer review
/// - <c>HighRiskReviewRate</c>             — % de mudanças de alto risco (BlastRadiusScore ≥ 50) com review
/// - <c>ContractChangeReviewRate</c>       — % de alterações de contrato com review
/// - <c>UnreviewedHighRiskChanges</c>      — mudanças sem review com BlastRadiusScore ≥ 50
/// - <c>BreakingContractChangesWithoutReview</c> — contratos com breaking change sem review
/// - <c>ReviewThrottleRisk</c>             — true se algum revisor tiver backlog &gt; 5 pending
///
/// Agrega:
/// - <c>TenantPeerReviewScore</c>          — (ReviewCoverageRate × 0.5 + HighRiskReviewRate × 0.5)
/// - <c>Tier</c>                           — Full / Good / Partial / AtRisk
///
/// Tier thresholds:
/// - <c>Full</c>    ≥ 95% reviewed
/// - <c>Good</c>    ≥ 75% reviewed
/// - <c>Partial</c> ≥ 50% reviewed
/// - <c>AtRisk</c>  &lt; 50% reviewed
///
/// Wave AP.2 — Collaborative Governance &amp; Workflow Automation (ChangeGovernance Compliance).
/// </summary>
public static class GetPeerReviewCoverageReport
{
    // ── Tier thresholds ────────────────────────────────────────────────────
    private const decimal FullThreshold = 95m;
    private const decimal GoodThreshold = 75m;
    private const decimal PartialThreshold = 50m;

    // ── High-risk change threshold ─────────────────────────────────────────
    private const int HighRiskBlastRadiusThreshold = 50;

    // ── Review backlog throttle threshold ─────────────────────────────────
    private const int ThrottleBacklogThreshold = 5;

    // ── Score weights ──────────────────────────────────────────────────────
    private const decimal ReviewCoverageWeight = 0.5m;
    private const decimal HighRiskReviewWeight = 0.5m;

    internal const int DefaultLookbackDays = 30;

    // ── Query ──────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela de análise em dias (7–90, default 30).</para>
    /// <para><c>TeamFilter</c>: filtro opcional por equipa (null = todas).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        string? TeamFilter = null) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Tier de completude de revisão entre pares.</summary>
    public enum ReviewCompletionTier
    {
        /// <summary>ReviewCoverageRate ≥ 95% — cobertura total.</summary>
        Full,
        /// <summary>ReviewCoverageRate ≥ 75% — boa cobertura.</summary>
        Good,
        /// <summary>ReviewCoverageRate ≥ 50% — cobertura parcial, melhoria necessária.</summary>
        Partial,
        /// <summary>ReviewCoverageRate &lt; 50% — cobertura em risco, acção urgente.</summary>
        AtRisk
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Mudança de alto risco sem peer review.</summary>
    public sealed record UnreviewedHighRiskChange(
        string ChangeId,
        string ServiceName,
        string TeamName,
        int BlastRadiusScore,
        int ConfidenceScore);

    /// <summary>Alteração de contrato com breaking change sem review.</summary>
    public sealed record BreakingContractChangeWithoutReview(
        string ContractId,
        string ContractName);

    /// <summary>Backlog de review de uma mudança.</summary>
    public sealed record ReviewBacklogItem(
        string ChangeId,
        string ServiceName,
        int PendingHours);

    /// <summary>Resultado do relatório de cobertura de peer review.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        decimal ReviewCoverageRatePct,
        decimal HighRiskReviewRatePct,
        decimal ContractChangeReviewRatePct,
        ReviewCompletionTier Tier,
        decimal TenantPeerReviewScore,
        bool ReviewThrottleRisk,
        IReadOnlyList<UnreviewedHighRiskChange> UnreviewedHighRiskChanges,
        IReadOnlyList<BreakingContractChangeWithoutReview> BreakingContractChangesWithoutReview,
        IReadOnlyList<ReviewBacklogItem> ReviewBacklogs);

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
        private readonly IPeerReviewCoverageReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(IPeerReviewCoverageReader reader, IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;

            var data = await _reader.GetByTenantAsync(
                query.TenantId, query.LookbackDays, cancellationToken);

            // Apply team filter when specified
            var changes = data.Changes;
            if (!string.IsNullOrWhiteSpace(query.TeamFilter))
            {
                changes = changes
                    .Where(c => string.Equals(c.TeamName, query.TeamFilter,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (changes.Count == 0 && data.ContractChanges.Count == 0)
            {
                return Result<Report>.Success(EmptyReport(now, query.TenantId, query.LookbackDays));
            }

            // ReviewCoverageRate = changes with peer review / total changes * 100
            int totalChanges = changes.Count;
            int reviewedChanges = changes.Count(c => c.HasPeerReview);
            decimal reviewCoverageRate = totalChanges > 0
                ? Math.Round((decimal)reviewedChanges / totalChanges * 100m, 1)
                : 100m;

            // HighRiskReviewRate = high-risk changes (BlastRadiusScore >= 50) with review / total high-risk * 100
            var highRiskChanges = changes.Where(c => c.BlastRadiusScore >= HighRiskBlastRadiusThreshold).ToList();
            int totalHighRisk = highRiskChanges.Count;
            int reviewedHighRisk = highRiskChanges.Count(c => c.HasPeerReview);
            decimal highRiskReviewRate = totalHighRisk > 0
                ? Math.Round((decimal)reviewedHighRisk / totalHighRisk * 100m, 1)
                : 100m;

            // ContractChangeReviewRate
            int totalContractChanges = data.ContractChanges.Count;
            int reviewedContractChanges = data.ContractChanges.Count(c => c.HasReview);
            decimal contractChangeReviewRate = totalContractChanges > 0
                ? Math.Round((decimal)reviewedContractChanges / totalContractChanges * 100m, 1)
                : 100m;

            // Tier based on ReviewCoverageRate
            var tier = ClassifyTier(reviewCoverageRate);

            // TenantPeerReviewScore
            decimal tenantScore = Math.Round(
                reviewCoverageRate * ReviewCoverageWeight + highRiskReviewRate * HighRiskReviewWeight, 1);

            // UnreviewedHighRiskChanges
            var unreviewedHighRisk = highRiskChanges
                .Where(c => !c.HasPeerReview)
                .Select(c => new UnreviewedHighRiskChange(
                    ChangeId: c.ChangeId,
                    ServiceName: c.ServiceName,
                    TeamName: c.TeamName,
                    BlastRadiusScore: c.BlastRadiusScore,
                    ConfidenceScore: c.ConfidenceScore))
                .ToList();

            // BreakingContractChangesWithoutReview
            var breakingWithoutReview = data.ContractChanges
                .Where(c => c.IsBreaking && !c.HasReview)
                .Select(c => new BreakingContractChangeWithoutReview(
                    ContractId: c.ContractId,
                    ContractName: c.ContractName))
                .ToList();

            // ReviewThrottleRisk = any reviewer has backlog > 5 pending
            bool throttleRisk = data.ReviewBacklogs.Count > ThrottleBacklogThreshold;

            // ReviewBacklogs
            var reviewBacklogs = data.ReviewBacklogs
                .Select(b => new ReviewBacklogItem(
                    ChangeId: b.ChangeId,
                    ServiceName: b.ServiceName,
                    PendingHours: b.PendingHours))
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                ReviewCoverageRatePct: reviewCoverageRate,
                HighRiskReviewRatePct: highRiskReviewRate,
                ContractChangeReviewRatePct: contractChangeReviewRate,
                Tier: tier,
                TenantPeerReviewScore: tenantScore,
                ReviewThrottleRisk: throttleRisk,
                UnreviewedHighRiskChanges: unreviewedHighRisk,
                BreakingContractChangesWithoutReview: breakingWithoutReview,
                ReviewBacklogs: reviewBacklogs));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        internal static ReviewCompletionTier ClassifyTier(decimal reviewCoverageRate) => reviewCoverageRate switch
        {
            >= FullThreshold => ReviewCompletionTier.Full,
            >= GoodThreshold => ReviewCompletionTier.Good,
            >= PartialThreshold => ReviewCompletionTier.Partial,
            _ => ReviewCompletionTier.AtRisk
        };

        private static Report EmptyReport(DateTimeOffset now, string tenantId, int lookbackDays)
            => new(
                GeneratedAt: now,
                TenantId: tenantId,
                LookbackDays: lookbackDays,
                ReviewCoverageRatePct: 100m,
                HighRiskReviewRatePct: 100m,
                ContractChangeReviewRatePct: 100m,
                Tier: ReviewCompletionTier.Full,
                TenantPeerReviewScore: 100m,
                ReviewThrottleRisk: false,
                UnreviewedHighRiskChanges: [],
                BreakingContractChangesWithoutReview: [],
                ReviewBacklogs: []);
    }
}
