using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetGovernanceEscalationReport;

/// <summary>
/// Feature: GetGovernanceEscalationReport — rastreabilidade de escalações de governança (Break Glass, JIT).
///
/// Analisa o comportamento de escalação de acesso privilegiado no tenant:
/// - <c>BreakGlassCount</c>              — total de eventos Break Glass no período
/// - <c>ProductionBreakGlassCount</c>    — eventos Break Glass em ambientes de produção
/// - <c>JitTotalRequests</c>             — total de pedidos JIT no período
/// - <c>JitApprovedRate</c>              — % de pedidos JIT aprovados
/// - <c>JitAutoApprovedRate</c>          — % de pedidos JIT auto-aprovados
/// - <c>JitNeverUsedCount</c>            — pedidos JIT aprovados mas nunca utilizados
/// - <c>TopEscalatingUsers</c>           — top 5 utilizadores por contagem de Break Glass
/// - <c>EscalationPatternFlags</c>       — flags de padrões anómalos detectados
///
/// Tier de risco:
/// - <c>Low</c>      &lt; 3 Break Glass no período
/// - <c>Medium</c>   3–9 Break Glass
/// - <c>High</c>     10–19 Break Glass
/// - <c>Critical</c> ≥ 20 Break Glass
///
/// TenantEscalationRiskScore = min(100, BreakGlassCount×5 + ProductionBreakGlassCount×3 + JitNeverUsedCount×2)
///
/// Wave AP.3 — Collaborative Governance &amp; Workflow Automation (ChangeGovernance Compliance).
/// </summary>
public static class GetGovernanceEscalationReport
{
    // ── Tier thresholds ────────────────────────────────────────────────────
    private const int LowThreshold = 3;
    private const int MediumThreshold = 10;
    private const int HighThreshold = 20;

    // ── Risk score weights ─────────────────────────────────────────────────
    private const int BreakGlassWeight = 5;
    private const int ProductionBreakGlassWeight = 3;
    private const int NeverUsedWeight = 2;
    private const int MaxRiskScore = 100;

    // ── Pattern flag thresholds ────────────────────────────────────────────
    private const decimal SurgeMultiplier = 1.5m;
    private const decimal JitAbuseThreshold = 80m;
    private const int UnusedPrivilegedThreshold = 5;

    private const int TopEscalatingUsersCount = 5;
    internal const int DefaultLookbackDays = 30;

    // ── Pattern flag constants ─────────────────────────────────────────────
    private const string FlagBreakGlassSurge = "BreakGlassSurge";
    private const string FlagProductionOnlyEscalations = "ProductionOnlyEscalations";
    private const string FlagJitAbuse = "JitAbuse";
    private const string FlagUnusedPrivilegedAccess = "UnusedPrivilegedAccess";

    // ── Query ──────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela de análise em dias (7–90, default 30).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays) : IQuery<Report>;

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Tier de risco de escalação baseado no número de eventos Break Glass.</summary>
    public enum EscalationRiskTier
    {
        /// <summary>Menos de 3 eventos Break Glass — risco baixo.</summary>
        Low,
        /// <summary>3 a 9 eventos Break Glass — risco médio.</summary>
        Medium,
        /// <summary>10 a 19 eventos Break Glass — risco alto.</summary>
        High,
        /// <summary>20 ou mais eventos Break Glass — risco crítico, acção urgente.</summary>
        Critical
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Utilizador com escalações de acesso privilegiado.</summary>
    public sealed record EscalatingUser(
        string UserId,
        string UserName,
        int BreakGlassCount);

    /// <summary>Resultado do relatório de escalações de governança.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        int BreakGlassCount,
        int ProductionBreakGlassCount,
        int JitTotalRequests,
        decimal JitApprovedRatePct,
        decimal JitAutoApprovedRatePct,
        int JitNeverUsedCount,
        EscalationRiskTier Tier,
        decimal TenantEscalationRiskScore,
        IReadOnlyList<EscalatingUser> TopEscalatingUsers,
        IReadOnlyList<string> EscalationPatternFlags,
        decimal? PreviousPeriodBreakGlassCount);

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
        private readonly IGovernanceEscalationReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(IGovernanceEscalationReader reader, IDateTimeProvider clock)
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

            int breakGlassCount = data.BreakGlassEvents.Count;
            int productionBreakGlassCount = data.BreakGlassEvents.Count(e => e.IsProduction);

            int jitTotal = data.JitAccessRequests.Count;
            int jitApproved = data.JitAccessRequests.Count(j => j.IsApproved);
            int jitAutoApproved = data.JitAccessRequests.Count(j => j.IsAutoApproved);
            int jitNeverUsed = data.JitAccessRequests
                .Count(j => j.IsApproved && !j.IsRejected && j.LastUsedAt == null);

            decimal jitApprovedRate = jitTotal > 0
                ? Math.Round((decimal)jitApproved / jitTotal * 100m, 1)
                : 0m;

            decimal jitAutoApprovedRate = jitTotal > 0
                ? Math.Round((decimal)jitAutoApproved / jitTotal * 100m, 1)
                : 0m;

            // Tier based on BreakGlassCount
            var tier = ClassifyTier(breakGlassCount);

            // TenantEscalationRiskScore = min(100, BreakGlassCount*5 + ProductionBreakGlassCount*3 + JitNeverUsedCount*2)
            decimal riskScore = Math.Min(MaxRiskScore,
                breakGlassCount * BreakGlassWeight
                + productionBreakGlassCount * ProductionBreakGlassWeight
                + jitNeverUsed * NeverUsedWeight);

            // TopEscalatingUsers = top 5 by BreakGlass count
            var topUsers = data.BreakGlassEvents
                .GroupBy(e => e.UserId)
                .Select(g => new EscalatingUser(
                    UserId: g.Key,
                    UserName: g.First().UserName,
                    BreakGlassCount: g.Count()))
                .OrderByDescending(u => u.BreakGlassCount)
                .Take(TopEscalatingUsersCount)
                .ToList();

            // EscalationPatternFlags
            var flags = BuildPatternFlags(
                breakGlassCount,
                productionBreakGlassCount,
                jitAutoApprovedRate,
                jitNeverUsed,
                data.PreviousPeriodBreakGlassCount);

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: query.TenantId,
                LookbackDays: query.LookbackDays,
                BreakGlassCount: breakGlassCount,
                ProductionBreakGlassCount: productionBreakGlassCount,
                JitTotalRequests: jitTotal,
                JitApprovedRatePct: jitApprovedRate,
                JitAutoApprovedRatePct: jitAutoApprovedRate,
                JitNeverUsedCount: jitNeverUsed,
                Tier: tier,
                TenantEscalationRiskScore: riskScore,
                TopEscalatingUsers: topUsers,
                EscalationPatternFlags: flags,
                PreviousPeriodBreakGlassCount: data.PreviousPeriodBreakGlassCount));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        internal static EscalationRiskTier ClassifyTier(int breakGlassCount) => breakGlassCount switch
        {
            < LowThreshold => EscalationRiskTier.Low,
            < MediumThreshold => EscalationRiskTier.Medium,
            < HighThreshold => EscalationRiskTier.High,
            _ => EscalationRiskTier.Critical
        };

        private static IReadOnlyList<string> BuildPatternFlags(
            int breakGlassCount,
            int productionBreakGlassCount,
            decimal jitAutoApprovedRate,
            int jitNeverUsed,
            decimal? previousPeriodBreakGlassCount)
        {
            var flags = new List<string>();

            // BreakGlassSurge: current > previous * 1.5
            if (previousPeriodBreakGlassCount.HasValue
                && previousPeriodBreakGlassCount.Value > 0
                && breakGlassCount > previousPeriodBreakGlassCount.Value * SurgeMultiplier)
            {
                flags.Add(FlagBreakGlassSurge);
            }

            // ProductionOnlyEscalations: all break glass events are production
            if (breakGlassCount > 0 && breakGlassCount == productionBreakGlassCount)
            {
                flags.Add(FlagProductionOnlyEscalations);
            }

            // JitAbuse: JitAutoApprovedRate > 80%
            if (jitAutoApprovedRate > JitAbuseThreshold)
            {
                flags.Add(FlagJitAbuse);
            }

            // UnusedPrivilegedAccess: JitNeverUsedCount > 5
            if (jitNeverUsed > UnusedPrivilegedThreshold)
            {
                flags.Add(FlagUnusedPrivilegedAccess);
            }

            return flags;
        }
    }
}
