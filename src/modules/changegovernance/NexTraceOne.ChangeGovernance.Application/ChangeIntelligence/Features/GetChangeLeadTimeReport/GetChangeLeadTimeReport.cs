using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetChangeLeadTimeReport;

/// <summary>
/// Feature: GetChangeLeadTimeReport — análise de lead time de mudanças por estágio de entrega.
///
/// Computa, por release, os tempos de cada estágio do pipeline:
/// - criação → pedido de aprovação
/// - aprovação solicitada → aprovação concedida
/// - aprovação → deploy pré-prod
/// - pré-prod → produção
/// - produção → verificação
///
/// Produz métricas agregadas de tenant: mediana, P95, tier DORA, gargalos de aprovação,
/// serviços mais lentos na promoção pré-prod→prod e tendência de lead time.
///
/// Wave AW.2 — Change Lead Time Report (ChangeGovernance ChangeIntelligence).
/// </summary>
public static class GetChangeLeadTimeReport
{
    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal em dias (1–365, default 90).</para>
    /// <para><c>ApprovalSlaHours</c>: SLA de aprovação em horas para identificar gargalos (default 24).</para>
    /// <para><c>BottleneckApprovalPct</c>: percentagem de lead time em aprovação que caracteriza gargalo (default 50).</para>
    /// <para><c>MaxServices</c>: número máximo de serviços no ranking (1–200, default 20).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int ApprovalSlaHours = 24,
        decimal BottleneckApprovalPct = 50m,
        int MaxServices = 20) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Tier de lead time DORA.</summary>
    public enum LeadTimeTier
    {
        /// <summary>Lead time médio ≤ 1 hora — excelência DORA.</summary>
        Elite,
        /// <summary>Lead time médio ≤ 24 horas.</summary>
        High,
        /// <summary>Lead time médio ≤ 7 dias.</summary>
        Medium,
        /// <summary>Lead time médio superior a 7 dias.</summary>
        Low
    }

    /// <summary>Duração de cada estágio de lead time de uma release (em minutos).</summary>
    public sealed record ReleaseStageDurations(
        Guid ReleaseId,
        string ServiceName,
        string TeamName,
        double? CreatedToApprovalRequested,
        double? ApprovalRequestedToApproved,
        double? ApprovedToPreProdDeploy,
        double? PreProdToProductionDeploy,
        double? ProductionDeployToVerification,
        double TotalLeadTime,
        string BottleneckStage);

    /// <summary>Resultado do relatório de lead time de mudanças.</summary>
    public sealed record Report(
        double MedianLeadTime,
        double P95LeadTime,
        LeadTimeTier TenantLeadTimeTier,
        IReadOnlyList<string> SlowestApprovalGroups,
        IReadOnlyList<string> SlowestPromotionServices,
        string LeadTimeTrend,
        decimal ApprovalBottleneckIndex,
        IReadOnlyDictionary<string, double> EnvironmentWaitTime,
        IReadOnlyList<ReleaseStageDurations> Releases,
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
        IChangeLeadTimeReader reader,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var to = clock.UtcNow;
            var from = to.AddDays(-query.LookbackDays);

            var entries = await reader.ListReleaseLeadTimesByTenantAsync(
                query.TenantId, from, to, cancellationToken);

            if (entries.Count == 0)
            {
                return Result<Report>.Success(new Report(
                    MedianLeadTime: 0,
                    P95LeadTime: 0,
                    TenantLeadTimeTier: LeadTimeTier.Low,
                    SlowestApprovalGroups: [],
                    SlowestPromotionServices: [],
                    LeadTimeTrend: "Stable",
                    ApprovalBottleneckIndex: 0m,
                    EnvironmentWaitTime: new Dictionary<string, double>(),
                    Releases: [],
                    TenantId: query.TenantId,
                    LookbackDays: query.LookbackDays,
                    From: from,
                    To: to,
                    GeneratedAt: clock.UtcNow));
            }

            // ── Per-release stage durations ───────────────────────────────────

            var stageDurations = entries.Select(e => ComputeStageDurations(e)).ToList();

            // ── Tenant aggregates ─────────────────────────────────────────────

            var allTotals = stageDurations.Select(s => s.TotalLeadTime).OrderBy(x => x).ToList();
            var medianLeadTime = Percentile(allTotals, 50);
            var p95LeadTime = Percentile(allTotals, 95);
            var tenantTier = ClassifyLeadTimeTier(medianLeadTime);

            // ── Slowest approval groups ───────────────────────────────────────

            var approvalSlaMins = query.ApprovalSlaHours * 60.0;
            var slowestApprovalGroups = stageDurations
                .Where(s => s.ApprovalRequestedToApproved.HasValue)
                .GroupBy(s => s.TeamName, StringComparer.OrdinalIgnoreCase)
                .Select(g => (TeamName: g.Key, AvgApprovalMins: g.Average(s => s.ApprovalRequestedToApproved!.Value)))
                .Where(x => x.AvgApprovalMins > approvalSlaMins)
                .OrderByDescending(x => x.AvgApprovalMins)
                .Take(query.MaxServices)
                .Select(x => x.TeamName)
                .ToList();

            // ── Slowest promotion services (PreProd → Prod > 24h) ─────────────

            var preProdSlaMinutes = 24 * 60.0;
            var slowestPromotionServices = stageDurations
                .Where(s => s.PreProdToProductionDeploy.HasValue)
                .GroupBy(s => s.ServiceName, StringComparer.OrdinalIgnoreCase)
                .Select(g => (ServiceName: g.Key, AvgMins: g.Average(s => s.PreProdToProductionDeploy!.Value)))
                .Where(x => x.AvgMins > preProdSlaMinutes)
                .OrderByDescending(x => x.AvgMins)
                .Take(query.MaxServices)
                .Select(x => x.ServiceName)
                .ToList();

            // ── Lead time trend (recent 30d vs 90d median) ────────────────────

            var recentCutoff = to.AddDays(-30);
            var recentEntries = entries.Where(e => e.CreatedAt >= recentCutoff).ToList();
            var recentDurations = stageDurations
                .Where(s => entries.First(e => e.ReleaseId == s.ReleaseId).CreatedAt >= recentCutoff)
                .Select(s => s.TotalLeadTime)
                .OrderBy(x => x)
                .ToList();

            var recentMedian = recentDurations.Count > 0 ? Percentile(recentDurations, 50) : medianLeadTime;
            var leadTimeTrend = recentMedian > medianLeadTime * 1.1 ? "Increasing"
                : recentMedian < medianLeadTime * 0.9 ? "Decreasing"
                : "Stable";

            // ── Approval bottleneck index ─────────────────────────────────────

            var approvalBottleneckIndex = 0m;
            var withBothApprovalAndTotal = stageDurations
                .Where(s => s.ApprovalRequestedToApproved.HasValue && s.TotalLeadTime > 0)
                .ToList();

            if (withBothApprovalAndTotal.Count > 0)
            {
                approvalBottleneckIndex = Math.Round(
                    (decimal)withBothApprovalAndTotal.Average(s =>
                        s.ApprovalRequestedToApproved!.Value / s.TotalLeadTime * 100.0), 2);
            }

            // ── Environment wait time ─────────────────────────────────────────

            var environmentWaitTime = entries
                .GroupBy(e => e.Environment, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var envDurations = g
                            .Select(e => ComputeStageDurations(e).TotalLeadTime)
                            .ToList();
                        return envDurations.Count > 0 ? envDurations.Average() : 0.0;
                    });

            return Result<Report>.Success(new Report(
                MedianLeadTime: medianLeadTime,
                P95LeadTime: p95LeadTime,
                TenantLeadTimeTier: tenantTier,
                SlowestApprovalGroups: slowestApprovalGroups,
                SlowestPromotionServices: slowestPromotionServices,
                LeadTimeTrend: leadTimeTrend,
                ApprovalBottleneckIndex: approvalBottleneckIndex,
                EnvironmentWaitTime: environmentWaitTime,
                Releases: stageDurations,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                From: from,
                To: to,
                GeneratedAt: clock.UtcNow));
        }

        private static ReleaseStageDurations ComputeStageDurations(LeadTimeEntry e)
        {
            double? createdToApproval = e.ApprovalRequestedAt.HasValue
                ? (e.ApprovalRequestedAt.Value - e.CreatedAt).TotalMinutes : null;
            double? approvalToApproved = e is { ApprovalRequestedAt: not null, ApprovedAt: not null }
                ? (e.ApprovedAt.Value - e.ApprovalRequestedAt.Value).TotalMinutes : null;
            double? approvedToPreProd = e is { ApprovedAt: not null, PreProdDeployedAt: not null }
                ? (e.PreProdDeployedAt.Value - e.ApprovedAt.Value).TotalMinutes : null;
            double? preProdToProd = e is { PreProdDeployedAt: not null, ProductionDeployedAt: not null }
                ? (e.ProductionDeployedAt.Value - e.PreProdDeployedAt.Value).TotalMinutes : null;
            double? prodToVerified = e is { ProductionDeployedAt: not null, VerifiedAt: not null }
                ? (e.VerifiedAt.Value - e.ProductionDeployedAt.Value).TotalMinutes : null;

            var totalLeadTime = (createdToApproval ?? 0)
                + (approvalToApproved ?? 0)
                + (approvedToPreProd ?? 0)
                + (preProdToProd ?? 0)
                + (prodToVerified ?? 0);

            var stages = new Dictionary<string, double>();
            if (createdToApproval.HasValue) stages["CreatedToApprovalRequested"] = createdToApproval.Value;
            if (approvalToApproved.HasValue) stages["ApprovalRequestedToApproved"] = approvalToApproved.Value;
            if (approvedToPreProd.HasValue) stages["ApprovedToPreProdDeploy"] = approvedToPreProd.Value;
            if (preProdToProd.HasValue) stages["PreProdToProductionDeploy"] = preProdToProd.Value;
            if (prodToVerified.HasValue) stages["ProductionDeployToVerification"] = prodToVerified.Value;

            var bottleneck = stages.Count > 0
                ? stages.OrderByDescending(kv => kv.Value).First().Key
                : "Unknown";

            return new ReleaseStageDurations(
                ReleaseId: e.ReleaseId,
                ServiceName: e.ServiceName,
                TeamName: e.TeamName,
                CreatedToApprovalRequested: createdToApproval,
                ApprovalRequestedToApproved: approvalToApproved,
                ApprovedToPreProdDeploy: approvedToPreProd,
                PreProdToProductionDeploy: preProdToProd,
                ProductionDeployToVerification: prodToVerified,
                TotalLeadTime: totalLeadTime,
                BottleneckStage: bottleneck);
        }

        private const double EliteThresholdMinutes = 60;
        private const double HighThresholdMinutes = 60 * 24;
        private const double MediumThresholdMinutes = 60 * 24 * 7;

        private static LeadTimeTier ClassifyLeadTimeTier(double medianMinutes) =>
            medianMinutes <= EliteThresholdMinutes ? LeadTimeTier.Elite
            : medianMinutes <= HighThresholdMinutes ? LeadTimeTier.High
            : medianMinutes <= MediumThresholdMinutes ? LeadTimeTier.Medium
            : LeadTimeTier.Low;

        private static double Percentile(List<double> sortedValues, int percentile)
        {
            if (sortedValues.Count == 0) return 0;
            if (sortedValues.Count == 1) return sortedValues[0];
            var index = (percentile / 100.0) * (sortedValues.Count - 1);
            var lower = (int)index;
            var upper = lower + 1;
            if (upper >= sortedValues.Count) return sortedValues[lower];
            var fraction = index - lower;
            return sortedValues[lower] + fraction * (sortedValues[upper] - sortedValues[lower]);
        }
    }
}
