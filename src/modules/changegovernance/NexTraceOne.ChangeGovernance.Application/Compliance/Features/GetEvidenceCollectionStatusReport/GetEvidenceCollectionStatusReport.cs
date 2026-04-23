using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetEvidenceCollectionStatusReport;

/// <summary>
/// Feature: GetEvidenceCollectionStatusReport — estado de recolha de evidências pré-auditoria.
///
/// Por standard:
/// - <c>EvidenceCompleteness</c>    — % de controlos com evidência válida
/// - <c>AuditReadinessTier</c>      — Ready / AlmostReady / NeedsWork / NotReady
/// - <c>EvidenceGapsByControl</c>   — controlos sem evidência recolhida
/// - <c>StaleEvidences</c>          — evidências com mais de <see cref="StaleEvidenceDays"/> dias
/// - <c>AutoCollectableEvidence</c> — controlos que podem ser preenchidos automaticamente
/// - <c>ManualEvidenceRequired</c>  — controlos que requerem evidência manual
/// - <c>DaysToAudit</c>             — dias até à próxima auditoria (null se não configurado)
///
/// Wave BB.2 — Compliance Automation &amp; Regulatory Reporting (ChangeGovernance/Foundation).
/// </summary>
public static class GetEvidenceCollectionStatusReport
{
    // ── Thresholds ─────────────────────────────────────────────────────────
    internal const int StaleEvidenceDays = 90;
    internal const decimal ReadyThreshold = 95m;
    internal const decimal AlmostReadyThreshold = 80m;
    internal const decimal NeedsWorkThreshold = 50m;
    internal const int DefaultLookbackDays = 90;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = DefaultLookbackDays) : IQuery<Report>;

    // ── Validator ──────────────────────────────────────────────────────────
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 365);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Tier de prontidão para auditoria.</summary>
    public enum AuditReadinessTier
    {
        /// <summary>≥ 95% dos controlos com evidência válida — pronto para auditoria.</summary>
        Ready,
        /// <summary>≥ 80% — quase pronto, poucos gaps a resolver.</summary>
        AlmostReady,
        /// <summary>≥ 50% — trabalho significativo necessário.</summary>
        NeedsWork,
        /// <summary>&lt; 50% — não está pronto para auditoria.</summary>
        NotReady
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Sumário de evidências para um standard específico.</summary>
    public sealed record StandardEvidenceSummary(
        string Standard,
        int TotalControls,
        int CollectedControls,
        int StaleControls,
        int AutoCollectableGaps,
        int ManualRequiredGaps,
        decimal EvidenceCompletenessPct,
        AuditReadinessTier Tier);

    /// <summary>Controlo com gap de evidência.</summary>
    public sealed record EvidenceGapItem(
        string ControlId,
        string ControlName,
        string Standard,
        bool IsAutoCollectable,
        DateTimeOffset? LastCollectedAt);

    /// <summary>Resultado do relatório de estado de recolha de evidências.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        int? DaysToAudit,
        decimal OverallEvidenceCompletenessPct,
        AuditReadinessTier OverallTier,
        int TotalStaleEvidences,
        int AutoCollectableCount,
        int ManualRequiredCount,
        IReadOnlyList<StandardEvidenceSummary> ByStandard,
        IReadOnlyList<EvidenceGapItem> EvidenceGapsByControl);

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler(
        IEvidenceCollectionStatusReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;

            var controls = await reader.ListByTenantAsync(request.TenantId, cancellationToken);
            var nextAudit = await reader.GetNextAuditDateAsync(request.TenantId, cancellationToken);

            int? daysToAudit = nextAudit.HasValue
                ? (int)Math.Ceiling((nextAudit.Value - now).TotalDays)
                : null;

            if (controls.Count == 0)
                return Result<Report>.Success(EmptyReport(now, request.TenantId, request.LookbackDays, daysToAudit));

            var staleThreshold = now.AddDays(-StaleEvidenceDays);

            // Group by standard
            var byStandard = controls
                .GroupBy(c => c.Standard)
                .Select(g =>
                {
                    var list = g.ToList();
                    int total = list.Count;
                    int collected = list.Count(c => c.IsCollected);
                    int stale = list.Count(c => c.IsCollected
                        && c.LastCollectedAt.HasValue
                        && c.LastCollectedAt.Value < staleThreshold);
                    int autoGaps = list.Count(c => !c.IsCollected && c.IsAutoCollectable);
                    int manualGaps = list.Count(c => !c.IsCollected && !c.IsAutoCollectable);
                    decimal pct = total > 0 ? (decimal)collected / total * 100m : 100m;
                    var tier = ClassifyTier(pct);
                    return new StandardEvidenceSummary(
                        Standard: g.Key,
                        TotalControls: total,
                        CollectedControls: collected,
                        StaleControls: stale,
                        AutoCollectableGaps: autoGaps,
                        ManualRequiredGaps: manualGaps,
                        EvidenceCompletenessPct: Math.Round(pct, 1),
                        Tier: tier);
                })
                .OrderBy(s => s.EvidenceCompletenessPct)
                .ToList();

            // Gap items
            var gapItems = controls
                .Where(c => !c.IsCollected)
                .Select(c => new EvidenceGapItem(
                    ControlId: c.ControlId,
                    ControlName: c.ControlName,
                    Standard: c.Standard,
                    IsAutoCollectable: c.IsAutoCollectable,
                    LastCollectedAt: c.LastCollectedAt))
                .ToList();

            int totalControls = controls.Count;
            int totalCollected = controls.Count(c => c.IsCollected);
            int staleTotal = controls.Count(c => c.IsCollected
                && c.LastCollectedAt.HasValue
                && c.LastCollectedAt.Value < staleThreshold);
            int autoCount = controls.Count(c => !c.IsCollected && c.IsAutoCollectable);
            int manualCount = controls.Count(c => !c.IsCollected && !c.IsAutoCollectable);
            decimal overallPct = totalControls > 0
                ? Math.Round((decimal)totalCollected / totalControls * 100m, 1)
                : 100m;

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: request.TenantId,
                LookbackDays: request.LookbackDays,
                DaysToAudit: daysToAudit,
                OverallEvidenceCompletenessPct: overallPct,
                OverallTier: ClassifyTier(overallPct),
                TotalStaleEvidences: staleTotal,
                AutoCollectableCount: autoCount,
                ManualRequiredCount: manualCount,
                ByStandard: byStandard,
                EvidenceGapsByControl: gapItems));
        }

        private static AuditReadinessTier ClassifyTier(decimal pct) => pct switch
        {
            _ when pct >= ReadyThreshold => AuditReadinessTier.Ready,
            _ when pct >= AlmostReadyThreshold => AuditReadinessTier.AlmostReady,
            _ when pct >= NeedsWorkThreshold => AuditReadinessTier.NeedsWork,
            _ => AuditReadinessTier.NotReady
        };

        private static Report EmptyReport(
            DateTimeOffset now, string tenantId, int lookbackDays, int? daysToAudit)
            => new(
                GeneratedAt: now,
                TenantId: tenantId,
                LookbackDays: lookbackDays,
                DaysToAudit: daysToAudit,
                OverallEvidenceCompletenessPct: 100m,
                OverallTier: AuditReadinessTier.Ready,
                TotalStaleEvidences: 0,
                AutoCollectableCount: 0,
                ManualRequiredCount: 0,
                ByStandard: [],
                EvidenceGapsByControl: []);
    }
}
