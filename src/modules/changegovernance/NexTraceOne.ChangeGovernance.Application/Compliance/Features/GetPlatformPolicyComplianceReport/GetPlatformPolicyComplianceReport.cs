using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetPlatformPolicyComplianceReport;

/// <summary>
/// Feature: GetPlatformPolicyComplianceReport — avaliação de conformidade com as políticas de plataforma.
///
/// Para cada <c>PolicyDefinition</c> activa no tenant:
/// - <c>PassRate</c>              — % de avaliações Passed no período
/// - <c>ViolatingEntities</c>    — serviços/equipas que falharam (com frequência e última data)
/// - <c>WorstOffenders</c>       — top 5 entidades com menor PassRate
/// - <c>PolicyComplianceTier</c> por política:
///   - <c>Enforced</c> ≥ 95% / <c>Partial</c> ≥ 75% / <c>AtRisk</c> ≥ 50% / <c>Failing</c> &lt; 50%
///
/// Agrega:
/// - <c>TenantPolicyComplianceScore</c> — média ponderada Mandatory (2×) + Advisory (1×)
/// - <c>PolicyComplianceDistribution</c> — % políticas por tier
/// - <c>EscalationRequired</c>          — políticas Mandatory com tier = Failing
///
/// Mapeia <c>PolicyDefinitionType</c> para tipo de governança:
/// - Mandatory (peso 2): PromotionGate, ComplianceCheck
/// - Advisory (peso 1):  AccessControl, FreezeWindow
/// - Informational:      AlertThreshold
///
/// Wave AJ.3 — Multi-Tenant Governance Intelligence (ChangeGovernance Compliance).
/// </summary>
public static class GetPlatformPolicyComplianceReport
{
    // ── Compliance tier thresholds ─────────────────────────────────────────
    private const decimal EnforcedThreshold = 95m;
    private const decimal PartialThreshold = 75m;
    private const decimal AtRiskThreshold = 50m;

    private const int WorstOffendersCount = 5;
    internal const int DefaultLookbackDays = 30;

    // ── Governance type weights ────────────────────────────────────────────
    private const decimal MandatoryWeight = 2m;
    private const decimal AdvisoryWeight = 1m;

    // ── Query ──────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela de análise em dias (7–90, default 30).</para>
    /// <para><c>MaxPolicies</c>: máximo de políticas no relatório (1–200, default 100).</para>
    /// <para><c>PolicyTypeFilter</c>: filtro opcional por tipo de política IA (null = todas).</para>
    /// <para><c>TeamFilter</c>: filtro opcional por equipa avaliada.</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        int MaxPolicies = 100,
        PolicyDefinitionType? PolicyTypeFilter = null,
        string? TeamFilter = null) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Tipo de governança de uma política para efeito de ponderação.</summary>
    public enum GovernancePolicyType
    {
        /// <summary>Política obrigatória — tem peso 2× no score global.</summary>
        Mandatory,
        /// <summary>Política aconselhada — tem peso 1× no score global.</summary>
        Advisory,
        /// <summary>Política informativa — não conta para o score global.</summary>
        Informational
    }

    /// <summary>Tier de conformidade de uma política.</summary>
    public enum PolicyComplianceTier
    {
        /// <summary>PassRate &lt; 50% — conformidade crítica, acção urgente.</summary>
        Failing,
        /// <summary>PassRate ≥ 50% — conformidade em risco.</summary>
        AtRisk,
        /// <summary>PassRate ≥ 75% — conformidade parcial, margem de melhoria.</summary>
        Partial,
        /// <summary>PassRate ≥ 95% — conformidade aplicada, nível de excelência.</summary>
        Enforced
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Entidade violadora de uma política.</summary>
    public sealed record ViolatingEntity(
        string EntityName,
        string EntityType,
        int FailureCount,
        int TotalEvaluations,
        decimal PassRatePct,
        DateTimeOffset LastFailureAt);

    /// <summary>Resultado de conformidade de uma política.</summary>
    public sealed record PolicyComplianceEntry(
        Guid PolicyDefinitionId,
        string PolicyName,
        PolicyDefinitionType PolicyType,
        GovernancePolicyType GovernanceType,
        bool IsEnabled,
        int EvaluationCount,
        int PassedCount,
        decimal PassRatePct,
        PolicyComplianceTier Tier,
        IReadOnlyList<ViolatingEntity> ViolatingEntities,
        IReadOnlyList<ViolatingEntity> WorstOffenders);

    /// <summary>Distribuição de políticas por tier de conformidade.</summary>
    public sealed record PolicyTierDistribution(
        int EnforcedCount,
        int PartialCount,
        int AtRiskCount,
        int FailingCount);

    /// <summary>Resultado do relatório de conformidade de políticas de plataforma.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        int TotalPoliciesAnalyzed,
        decimal TenantPolicyComplianceScore,
        PolicyTierDistribution Distribution,
        IReadOnlyList<PolicyComplianceEntry> Policies,
        IReadOnlyList<PolicyComplianceEntry> EscalationRequired);

    // ── Validator ──────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 90);
            RuleFor(q => q.MaxPolicies).InclusiveBetween(1, 200);
        }
    }

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IPolicyDefinitionRepository _policyRepo;
        private readonly IPolicyEvaluationHistoryReader _evaluationReader;
        private readonly IDateTimeProvider _clock;

        public Handler(
            IPolicyDefinitionRepository policyRepo,
            IPolicyEvaluationHistoryReader evaluationReader,
            IDateTimeProvider clock)
        {
            _policyRepo = Guard.Against.Null(policyRepo);
            _evaluationReader = Guard.Against.Null(evaluationReader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var from = now.AddDays(-query.LookbackDays);

            // 1. Load active policies for this tenant
            var allPolicies = await _policyRepo.ListByTenantAsync(
                query.TenantId, query.PolicyTypeFilter, cancellationToken);

            var activePolicies = allPolicies
                .Where(p => p.IsEnabled)
                .Take(query.MaxPolicies)
                .ToList();

            if (activePolicies.Count == 0)
            {
                return Result<Report>.Success(EmptyReport(now, query.TenantId, query.LookbackDays));
            }

            // 2. Load evaluation history for the period
            var evaluations = await _evaluationReader.ListEvaluationsAsync(
                query.TenantId, from, now, cancellationToken);

            // Group evaluations by policy
            var byPolicy = evaluations
                .GroupBy(e => e.PolicyDefinitionId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 3. Build per-policy compliance entries
            var entries = new List<PolicyComplianceEntry>(activePolicies.Count);

            foreach (var policy in activePolicies)
            {
                byPolicy.TryGetValue(policy.Id.Value, out var policyEvals);
                policyEvals ??= [];

                // Apply team filter if specified
                if (!string.IsNullOrWhiteSpace(query.TeamFilter))
                {
                    policyEvals = policyEvals
                        .Where(e => string.Equals(e.EntityName, query.TeamFilter,
                            StringComparison.OrdinalIgnoreCase)
                            || string.Equals(e.EntityType, "team", StringComparison.OrdinalIgnoreCase)
                                && string.Equals(e.EntityName, query.TeamFilter,
                                    StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                int evalCount = policyEvals.Count;
                int passedCount = policyEvals.Count(e => e.Passed);
                decimal passRate = evalCount > 0
                    ? Math.Round((decimal)passedCount / evalCount * 100m, 1)
                    : 100m; // No evaluations → assume compliant (honest-null)

                var tier = ClassifyTier(passRate);
                var govType = MapGovernanceType(policy.PolicyType);

                // Build violating entities (failed evaluations grouped by entity)
                var violatingEntities = policyEvals
                    .Where(e => !e.Passed)
                    .GroupBy(e => (e.EntityName, e.EntityType))
                    .Select(g =>
                    {
                        int failures = g.Count();
                        var allForEntity = policyEvals.Count(e =>
                            string.Equals(e.EntityName, g.Key.EntityName, StringComparison.OrdinalIgnoreCase));
                        decimal entityPassRate = allForEntity > 0
                            ? Math.Round((1m - (decimal)failures / allForEntity) * 100m, 1)
                            : 0m;
                        return new ViolatingEntity(
                            EntityName: g.Key.EntityName,
                            EntityType: g.Key.EntityType,
                            FailureCount: failures,
                            TotalEvaluations: allForEntity,
                            PassRatePct: entityPassRate,
                            LastFailureAt: g.Max(e => e.EvaluatedAt));
                    })
                    .OrderBy(v => v.PassRatePct)
                    .ThenByDescending(v => v.FailureCount)
                    .ToList();

                var worstOffenders = violatingEntities
                    .Take(WorstOffendersCount)
                    .ToList();

                entries.Add(new PolicyComplianceEntry(
                    PolicyDefinitionId: policy.Id.Value,
                    PolicyName: policy.Name,
                    PolicyType: policy.PolicyType,
                    GovernanceType: govType,
                    IsEnabled: policy.IsEnabled,
                    EvaluationCount: evalCount,
                    PassedCount: passedCount,
                    PassRatePct: passRate,
                    Tier: tier,
                    ViolatingEntities: violatingEntities,
                    WorstOffenders: worstOffenders));
            }

            // 4. Compute tenant compliance score (Mandatory 2×, Advisory 1×, Informational excluded)
            decimal tenantScore = ComputeTenantScore(entries);

            // 5. Distribution
            var distribution = new PolicyTierDistribution(
                EnforcedCount: entries.Count(e => e.Tier == PolicyComplianceTier.Enforced),
                PartialCount: entries.Count(e => e.Tier == PolicyComplianceTier.Partial),
                AtRiskCount: entries.Count(e => e.Tier == PolicyComplianceTier.AtRisk),
                FailingCount: entries.Count(e => e.Tier == PolicyComplianceTier.Failing));

            // 6. EscalationRequired: Mandatory policies with Failing tier
            var escalation = entries
                .Where(e => e.GovernanceType == GovernancePolicyType.Mandatory
                    && e.Tier == PolicyComplianceTier.Failing)
                .OrderBy(e => e.PassRatePct)
                .ThenBy(e => e.PolicyName)
                .ToList();

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                TotalPoliciesAnalyzed: entries.Count,
                TenantPolicyComplianceScore: tenantScore,
                Distribution: distribution,
                Policies: entries,
                EscalationRequired: escalation));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        internal static PolicyComplianceTier ClassifyTier(decimal passRate) => passRate switch
        {
            >= EnforcedThreshold => PolicyComplianceTier.Enforced,
            >= PartialThreshold => PolicyComplianceTier.Partial,
            >= AtRiskThreshold => PolicyComplianceTier.AtRisk,
            _ => PolicyComplianceTier.Failing
        };

        internal static GovernancePolicyType MapGovernanceType(PolicyDefinitionType type) => type switch
        {
            PolicyDefinitionType.PromotionGate or PolicyDefinitionType.ComplianceCheck => GovernancePolicyType.Mandatory,
            PolicyDefinitionType.AccessControl or PolicyDefinitionType.FreezeWindow => GovernancePolicyType.Advisory,
            _ => GovernancePolicyType.Informational
        };

        private static decimal ComputeTenantScore(IReadOnlyList<PolicyComplianceEntry> entries)
        {
            var scoreable = entries
                .Where(e => e.GovernanceType != GovernancePolicyType.Informational)
                .ToList();

            if (scoreable.Count == 0) return 100m;

            decimal weightedSum = scoreable.Sum(e =>
                e.PassRatePct * (e.GovernanceType == GovernancePolicyType.Mandatory
                    ? MandatoryWeight : AdvisoryWeight));

            decimal totalWeight = scoreable.Sum(e =>
                e.GovernanceType == GovernancePolicyType.Mandatory
                    ? MandatoryWeight : AdvisoryWeight);

            return Math.Round(weightedSum / totalWeight, 1);
        }

        private static Report EmptyReport(DateTimeOffset now, string tenantId, int lookbackDays)
            => new(
                GeneratedAt: now,
                TenantId: tenantId,
                LookbackDays: lookbackDays,
                TotalPoliciesAnalyzed: 0,
                TenantPolicyComplianceScore: 100m,
                Distribution: new PolicyTierDistribution(0, 0, 0, 0),
                Policies: [],
                EscalationRequired: []);
    }
}
