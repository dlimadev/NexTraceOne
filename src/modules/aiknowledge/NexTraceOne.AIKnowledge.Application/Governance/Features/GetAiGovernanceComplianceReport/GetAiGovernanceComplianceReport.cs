using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiGovernanceComplianceReport;

/// <summary>
/// Feature: GetAiGovernanceComplianceReport — compliance de governança de IA.
///
/// Verifica se os modelos em uso no tenant cumprem os requisitos de governança:
/// aprovação formal, audit trail, budget de tokens, política de acesso e revisão periódica.
///
/// <c>ModelGovernanceTier</c>:
/// - <c>Compliant</c>   — HasApproval + HasAuditTrail + BudgetCompliance ≥95% + !ReviewOverdue
/// - <c>Partial</c>     — HasApproval + HasAuditTrail, mas BudgetCompliance &lt;95% ou ReviewOverdue
/// - <c>NonCompliant</c>— sem aprovação formal ou sem audit trail
/// - <c>Untracked</c>   — sem dados de governança disponíveis
///
/// Endpoint: GET /api/v1/ai/governance/compliance-report
/// Wave AT.3 — AI Model Quality &amp; Drift Governance (AIKnowledge Governance).
/// </summary>
public static class GetAiGovernanceComplianceReport
{
    // ── Configuration keys ─────────────────────────────────────────────────
    internal const string ModelReviewDaysKey = "ai.governance.model_review_days";
    internal const string BudgetOverrunThresholdKey = "ai.governance.budget_overrun_threshold";
    internal const string AuditTrailLookbackDaysKey = "ai.governance.audit_trail_lookback_days";
    internal const int DefaultModelReviewDays = 90;
    internal const int DefaultBudgetOverrunThreshold = 2;
    internal const int DefaultAuditTrailLookbackDays = 30;

    // ── Query ──────────────────────────────────────────────────────────────
    /// <summary>Query para o relatório de compliance de governança de IA.</summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultAuditTrailLookbackDays) : IQuery<Report>;

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
    /// <summary>Tier de governança de modelo de IA.</summary>
    public enum ModelGovernanceTier { Compliant, Partial, NonCompliant, Untracked }

    // ── Value objects ──────────────────────────────────────────────────────
    /// <summary>Linha de compliance por modelo activo.</summary>
    public sealed record ModelComplianceRow(
        Guid ModelId,
        string ModelName,
        bool HasFormalApproval,
        bool HasAuditTrail,
        double BudgetComplianceRate,
        double PolicyAdherence,
        DateTimeOffset? LastReviewDate,
        bool ReviewOverdue,
        int BudgetOverrunPeriods,
        ModelGovernanceTier Tier);

    /// <summary>Violação de política de acesso.</summary>
    public sealed record PolicyViolation(
        Guid ModelId,
        string ModelName,
        string ViolationType,
        int ViolationCount,
        DateTimeOffset LastViolationAt);

    /// <summary>Gaps de compliance do tenant.</summary>
    public sealed record ComplianceGaps(
        IReadOnlyList<ModelComplianceRow> ModelsWithoutApproval,
        IReadOnlyList<ModelComplianceRow> ModelsWithoutAuditTrail,
        IReadOnlyList<ModelComplianceRow> BudgetOverrunModels,
        IReadOnlyList<PolicyViolation> PolicyViolatingCalls);

    /// <summary>Relatório completo de compliance de governança de IA.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<ModelComplianceRow> ByModel,
        double TenantAiGovernanceScore,
        double AiGovernanceComplianceIndex,
        ComplianceGaps Gaps,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    /// <summary>Handler da query <see cref="Query"/>.</summary>
    public sealed class Handler(
        IAiGovernanceComplianceReader complianceReader,
        IConfigurationResolutionService configService,
        NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;

            // Resolve config
            var reviewDaysCfg = await configService.ResolveEffectiveValueAsync(
                ModelReviewDaysKey, ConfigurationScope.System, null, cancellationToken);
            var overrunThresholdCfg = await configService.ResolveEffectiveValueAsync(
                BudgetOverrunThresholdKey, ConfigurationScope.System, null, cancellationToken);
            var auditLookbackCfg = await configService.ResolveEffectiveValueAsync(
                AuditTrailLookbackDaysKey, ConfigurationScope.System, null, cancellationToken);

            var reviewDays = int.TryParse(reviewDaysCfg?.EffectiveValue, out var rd) ? rd : DefaultModelReviewDays;
            var overrunThreshold = int.TryParse(overrunThresholdCfg?.EffectiveValue, out var ot) ? ot : DefaultBudgetOverrunThreshold;
            var auditLookback = int.TryParse(auditLookbackCfg?.EffectiveValue, out var al) ? al : DefaultAuditTrailLookbackDays;

            var rows = await complianceReader.GetComplianceRowsAsync(
                request.TenantId, auditLookback, reviewDays, cancellationToken);

            // Classify tiers
            var classifiedRows = rows
                .Select(r => r with { Tier = ClassifyTier(r) })
                .ToList();

            // Violations
            var from = now.AddDays(-request.LookbackDays);
            var violations = await complianceReader.GetPolicyViolationsAsync(
                request.TenantId, from, now, cancellationToken);

            // Scores
            var total = classifiedRows.Count;
            var compliantOrPartial = classifiedRows.Count(r =>
                r.Tier is ModelGovernanceTier.Compliant or ModelGovernanceTier.Partial);
            var compliantOnly = classifiedRows.Count(r => r.Tier == ModelGovernanceTier.Compliant);

            var governanceScore = total > 0
                ? Math.Round((double)compliantOrPartial / total * 100, 1) : 0.0;
            var complianceIndex = total > 0
                ? Math.Round((double)compliantOnly / total * 100, 1) : 0.0;

            // Gaps
            var gaps = new ComplianceGaps(
                ModelsWithoutApproval: classifiedRows.Where(r => !r.HasFormalApproval).ToList(),
                ModelsWithoutAuditTrail: classifiedRows.Where(r => !r.HasAuditTrail).ToList(),
                BudgetOverrunModels: classifiedRows.Where(r => r.BudgetOverrunPeriods >= overrunThreshold).ToList(),
                PolicyViolatingCalls: violations);

            return Result<Report>.Success(new Report(
                TenantId: request.TenantId,
                ByModel: classifiedRows,
                TenantAiGovernanceScore: governanceScore,
                AiGovernanceComplianceIndex: complianceIndex,
                Gaps: gaps,
                GeneratedAt: now));
        }

        private static ModelGovernanceTier ClassifyTier(ModelComplianceRow row)
        {
            // Untracked — no governance data
            if (!row.HasFormalApproval && !row.HasAuditTrail && row.BudgetComplianceRate == 0 && row.PolicyAdherence == 0)
                return ModelGovernanceTier.Untracked;

            // NonCompliant — missing approval or audit trail
            if (!row.HasFormalApproval || !row.HasAuditTrail)
                return ModelGovernanceTier.NonCompliant;

            // Compliant — all checks pass
            if (row.BudgetComplianceRate >= 95.0 && !row.ReviewOverdue)
                return ModelGovernanceTier.Compliant;

            // Partial — approval + audit trail but budget or review issues
            return ModelGovernanceTier.Partial;
        }
    }
}
