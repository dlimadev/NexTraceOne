using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetApprovalWorkflowReport;

/// <summary>
/// Feature: GetApprovalWorkflowReport — eficiência de workflows de aprovação por ambiente e tenant.
///
/// Por cada ambiente com entradas de aprovação:
/// - <c>AvgApprovalTimeHours</c>     — tempo médio de aprovação em horas
/// - <c>SlaComplianceRatePct</c>     — % de aprovações dentro do SLA
/// - <c>AutoApprovalRatePct</c>      — % de aprovações automáticas
/// - <c>RejectionRatePct</c>         — % de rejeições
/// - <c>PendingCount</c>             — aprovações pendentes
/// - <c>ApprovalTier</c>             — Efficient / Normal / Delayed / Blocked
///
/// Agrega a nível de tenant:
/// - <c>TenantApprovalHealthScore</c> — média ponderada de SlaComplianceRate (prod 2×, non-prod 1×)
/// - <c>ApprovalEfficiencyIndex</c>   — % de aprovações que cumpriram o SLA (weighted)
/// - <c>AutoApprovalRatePct</c>       — média ponderada de AutoApprovalRate
/// - <c>BottleneckApprovers</c>       — top 5 aprovadores por PendingCount (todos os ambientes)
/// - <c>TotalPendingApprovals</c>     — soma de todos os pending
/// - <c>Tier</c>                      — tier global baseado em AvgApprovalTimeHours ponderado
///
/// Wave AP.1 — Collaborative Governance &amp; Workflow Automation (ChangeGovernance Compliance).
/// </summary>
public static class GetApprovalWorkflowReport
{
    // ── Tier thresholds (hours) ────────────────────────────────────────────
    private const decimal EfficientThreshold = 4m;
    private const decimal NormalThreshold = 12m;
    private const decimal DelayedThreshold = 48m;

    // ── Weights ────────────────────────────────────────────────────────────
    private const decimal ProductionWeight = 2m;
    private const decimal NonProductionWeight = 1m;

    private const int BottleneckApproversCount = 5;
    internal const int DefaultLookbackDays = 30;

    // ── Query ──────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela de análise em dias (7–90, default 30).</para>
    /// <para><c>EnvironmentFilter</c>: filtro opcional por ambiente (null = todos).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays,
        string? EnvironmentFilter = null) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Tier de eficiência de aprovação baseado no tempo médio.</summary>
    public enum ApprovalTier
    {
        /// <summary>AvgApprovalTimeHours &lt; 4h — aprovação eficiente.</summary>
        Efficient,
        /// <summary>AvgApprovalTimeHours &lt; 12h — aprovação dentro do normal.</summary>
        Normal,
        /// <summary>AvgApprovalTimeHours &lt; 48h — aprovação atrasada, atenção necessária.</summary>
        Delayed,
        /// <summary>AvgApprovalTimeHours ≥ 48h — aprovação bloqueada, acção urgente.</summary>
        Blocked
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Aprovador com backlog de aprovações pendentes.</summary>
    public sealed record BottleneckApprover(
        string ApproverId,
        string ApproverName,
        int PendingCount);

    /// <summary>Sumário de aprovações por ambiente.</summary>
    public sealed record EnvironmentApprovalSummary(
        string EnvironmentName,
        string ApprovalType,
        int TotalApprovals,
        decimal AvgApprovalTimeHours,
        ApprovalTier Tier,
        decimal SlaComplianceRatePct,
        decimal AutoApprovalRatePct,
        decimal RejectionRatePct,
        int PendingCount);

    /// <summary>Resultado do relatório de eficiência de workflows de aprovação.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        decimal TenantApprovalHealthScore,
        decimal ApprovalEfficiencyIndex,
        ApprovalTier Tier,
        decimal AutoApprovalRatePct,
        IReadOnlyList<BottleneckApprover> BottleneckApprovers,
        IReadOnlyList<EnvironmentApprovalSummary> Environments,
        int TotalPendingApprovals);

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
        private readonly IApprovalWorkflowReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(IApprovalWorkflowReader reader, IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;

            var entries = await _reader.ListByTenantAsync(
                query.TenantId, query.LookbackDays, cancellationToken);

            // Apply environment filter when specified
            if (!string.IsNullOrWhiteSpace(query.EnvironmentFilter))
            {
                entries = entries
                    .Where(e => string.Equals(e.Environment, query.EnvironmentFilter,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (entries.Count == 0)
            {
                return Result<Report>.Success(EmptyReport(now, query.TenantId, query.LookbackDays));
            }

            // Build per-environment summaries
            var summaries = entries.Select(e => new EnvironmentApprovalSummary(
                EnvironmentName: e.Environment,
                ApprovalType: e.ApprovalType,
                TotalApprovals: e.TotalApprovals,
                AvgApprovalTimeHours: e.AvgApprovalTimeHours,
                Tier: ClassifyTier(e.AvgApprovalTimeHours),
                SlaComplianceRatePct: Math.Round(e.SlaComplianceRate * 100m, 1),
                AutoApprovalRatePct: Math.Round(e.AutoApprovalRate * 100m, 1),
                RejectionRatePct: Math.Round(e.RejectionRate * 100m, 1),
                PendingCount: e.PendingCount)).ToList();

            // Compute TenantApprovalHealthScore = weighted avg SlaComplianceRate
            decimal healthScore = ComputeWeightedScore(entries,
                e => e.SlaComplianceRate * 100m, e => IsProduction(e.Environment));

            // Compute ApprovalEfficiencyIndex = % of approvals meeting SLA (weighted by TotalApprovals)
            decimal efficiencyIndex = ComputeApprovalEfficiencyIndex(entries);

            // Compute AutoApprovalRate tenant-wide (weighted avg)
            decimal autoApprovalRate = ComputeWeightedScore(entries,
                e => e.AutoApprovalRate * 100m, e => IsProduction(e.Environment));

            // Compute global Tier using weighted avg AvgApprovalTimeHours
            decimal weightedAvgTime = ComputeWeightedAvgTime(entries);
            var globalTier = ClassifyTier(weightedAvgTime);

            // BottleneckApprovers = top 5 by PendingCount across all environments
            var bottleneckApprovers = entries
                .SelectMany(e => e.ApproverBacklogs)
                .GroupBy(b => b.ApproverId)
                .Select(g => new BottleneckApprover(
                    ApproverId: g.Key,
                    ApproverName: g.First().ApproverName,
                    PendingCount: g.Sum(b => b.PendingCount)))
                .OrderByDescending(b => b.PendingCount)
                .Take(BottleneckApproversCount)
                .ToList();

            int totalPending = summaries.Sum(s => s.PendingCount);

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                TenantApprovalHealthScore: Math.Round(healthScore, 1),
                ApprovalEfficiencyIndex: Math.Round(efficiencyIndex, 1),
                Tier: globalTier,
                AutoApprovalRatePct: Math.Round(autoApprovalRate, 1),
                BottleneckApprovers: bottleneckApprovers,
                Environments: summaries,
                TotalPendingApprovals: totalPending));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        internal static ApprovalTier ClassifyTier(decimal avgHours) => avgHours switch
        {
            < EfficientThreshold => ApprovalTier.Efficient,
            < NormalThreshold => ApprovalTier.Normal,
            < DelayedThreshold => ApprovalTier.Delayed,
            _ => ApprovalTier.Blocked
        };

        private static bool IsProduction(string environment)
            => environment.Contains("prod", StringComparison.OrdinalIgnoreCase);

        private static decimal GetWeight(IApprovalWorkflowReader.ApprovalEnvironmentEntry entry)
            => IsProduction(entry.Environment) ? ProductionWeight : NonProductionWeight;

        private static decimal ComputeWeightedScore(
            IReadOnlyList<IApprovalWorkflowReader.ApprovalEnvironmentEntry> entries,
            Func<IApprovalWorkflowReader.ApprovalEnvironmentEntry, decimal> scoreSelector,
            Func<IApprovalWorkflowReader.ApprovalEnvironmentEntry, bool> isProdSelector)
        {
            if (entries.Count == 0) return 100m;

            decimal weightedSum = entries.Sum(e =>
                scoreSelector(e) * (isProdSelector(e) ? ProductionWeight : NonProductionWeight));
            decimal totalWeight = entries.Sum(e =>
                isProdSelector(e) ? ProductionWeight : NonProductionWeight);

            return totalWeight > 0 ? weightedSum / totalWeight : 100m;
        }

        private static decimal ComputeApprovalEfficiencyIndex(
            IReadOnlyList<IApprovalWorkflowReader.ApprovalEnvironmentEntry> entries)
        {
            // Weighted by TotalApprovals: sum(SlaComplianceRate * TotalApprovals) / sum(TotalApprovals)
            int totalApprovals = entries.Sum(e => e.TotalApprovals);
            if (totalApprovals == 0) return 100m;

            decimal weightedSum = entries.Sum(e => e.SlaComplianceRate * e.TotalApprovals);
            return weightedSum / totalApprovals * 100m;
        }

        private static decimal ComputeWeightedAvgTime(
            IReadOnlyList<IApprovalWorkflowReader.ApprovalEnvironmentEntry> entries)
        {
            if (entries.Count == 0) return 0m;

            decimal weightedSum = entries.Sum(e => e.AvgApprovalTimeHours * GetWeight(e));
            decimal totalWeight = entries.Sum(GetWeight);

            return totalWeight > 0 ? weightedSum / totalWeight : 0m;
        }

        private static Report EmptyReport(DateTimeOffset now, string tenantId, int lookbackDays)
            => new(
                GeneratedAt: now,
                TenantId: tenantId,
                LookbackDays: lookbackDays,
                TenantApprovalHealthScore: 100m,
                ApprovalEfficiencyIndex: 100m,
                Tier: ApprovalTier.Efficient,
                AutoApprovalRatePct: 0m,
                BottleneckApprovers: [],
                Environments: [],
                TotalPendingApprovals: 0);
    }
}
