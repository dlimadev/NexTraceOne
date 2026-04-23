using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetCrossStandardComplianceGapReport;

/// <summary>
/// Feature: GetCrossStandardComplianceGapReport — gaps de compliance que afectam múltiplos standards simultaneamente.
///
/// Por cada gap identifica:
/// - <c>AffectedStandards</c>  — lista de standards impactados (GDPR/HIPAA/PCI-DSS/FedRAMP/CMMC)
/// - <c>ImpactScore</c>         — AffectedStandardsCount × ServiceTierWeight (Critical=3/Standard=2/Internal=1)
/// - <c>GapType</c>             — Technical / Process / Evidence
/// - <c>CrossStandardGapMatrix</c> — N×M: gap × standard (bool)
/// - <c>TenantCompliancePriorityList</c> — top N gaps por ImpactScore/RemediationComplexity
/// - <c>EstimatedComplianceLift</c> — % de score total recuperável ao remediar top-10 gaps
///
/// <c>CrossStandardGapTier</c>: Minimal / Partial / Significant / Critical
/// <c>TenantCrossComplianceScore</c> = % gaps remediados ponderados por ImpactScore
///
/// Wave BB.1 — Compliance Automation &amp; Regulatory Reporting (ChangeGovernance Compliance).
/// </summary>
public static class GetCrossStandardComplianceGapReport
{
    // ── Tier thresholds ────────────────────────────────────────────────────
    internal const int MinimalThreshold = 5;
    internal const int PartialThreshold = 15;
    internal const int SignificantThreshold = 30;

    // ── Service tier weights ───────────────────────────────────────────────
    private const decimal CriticalTierWeight = 3m;
    private const decimal StandardTierWeight = 2m;
    private const decimal InternalTierWeight = 1m;

    internal const int DefaultTopPriorityCount = 10;
    internal const int DefaultLookbackDays = 90;

    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        IReadOnlyList<string>? Standards = null,
        int TopPriorityCount = DefaultTopPriorityCount,
        int LookbackDays = DefaultLookbackDays) : IQuery<Report>;

    // ── Validator ──────────────────────────────────────────────────────────
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(q => q.TopPriorityCount).InclusiveBetween(1, 50);
            RuleFor(q => q.LookbackDays).InclusiveBetween(7, 365);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────

    /// <summary>Classificação do nível de gaps cross-standard do tenant.</summary>
    public enum CrossStandardGapTier
    {
        /// <summary>Poucos gaps cross-standard — postura de compliance forte.</summary>
        Minimal,
        /// <summary>Alguns gaps cross-standard — atenção recomendada.</summary>
        Partial,
        /// <summary>Gaps cross-standard relevantes — acção necessária.</summary>
        Significant,
        /// <summary>Muitos gaps críticos cross-standard — remediação urgente.</summary>
        Critical
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Gap identificado que afecta múltiplos standards.</summary>
    public sealed record CrossStandardGap(
        string GapId,
        string GapName,
        string GapType,
        IReadOnlyList<string> AffectedStandards,
        int AffectedStandardsCount,
        decimal ImpactScore,
        int AffectedServicesCount,
        int RemediationComplexity,
        bool IsRemediated);

    /// <summary>Item de prioridade de remediação (ImpactScore / RemediationComplexity).</summary>
    public sealed record CompliancePriorityItem(
        string GapId,
        string GapName,
        decimal ImpactScore,
        int RemediationComplexity,
        decimal PriorityScore,
        IReadOnlyList<string> AffectedStandards);

    /// <summary>Célula da gap matrix (gap × standard).</summary>
    public sealed record GapMatrixCell(string GapId, string Standard, bool IsAffected);

    /// <summary>Resultado do relatório de gaps cross-standard.</summary>
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        IReadOnlyList<string> Standards,
        int LookbackDays,
        decimal TenantCrossComplianceScore,
        CrossStandardGapTier Tier,
        int TotalGaps,
        int RemediatedGaps,
        int CrossStandardGapsCount,
        decimal EstimatedComplianceLift,
        IReadOnlyList<CrossStandardGap> CrossStandardGaps,
        IReadOnlyList<CompliancePriorityItem> TenantCompliancePriorityList,
        IReadOnlyList<GapMatrixCell> CrossStandardGapMatrix);

    // ── Handler ────────────────────────────────────────────────────────────

    public sealed class Handler(
        ICrossStandardComplianceGapReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        private static readonly IReadOnlyList<string> DefaultStandards =
            ["GDPR", "HIPAA", "PCI-DSS", "FedRAMP", "CMMC"];

        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var standards = request.Standards is { Count: > 0 }
                ? request.Standards
                : DefaultStandards;

            var now = clock.UtcNow;
            var gaps = await reader.ListGapsByTenantAsync(request.TenantId, standards, cancellationToken);

            if (gaps.Count == 0)
                return Result<Report>.Success(EmptyReport(now, request.TenantId, standards, request.LookbackDays));

            var crossGaps = gaps
                .Where(g => g.AffectedStandards.Count > 1)
                .Select(g => new CrossStandardGap(
                    GapId: g.GapId,
                    GapName: g.GapName,
                    GapType: g.GapType,
                    AffectedStandards: g.AffectedStandards,
                    AffectedStandardsCount: g.AffectedStandards.Count,
                    ImpactScore: ComputeImpactScore(g),
                    AffectedServicesCount: g.AffectedServiceIds.Count,
                    RemediationComplexity: g.RemediationComplexity,
                    IsRemediated: g.IsRemediated))
                .OrderByDescending(g => g.ImpactScore)
                .ToList();

            var priorityList = crossGaps
                .Where(g => !g.IsRemediated)
                .OrderByDescending(g => g.RemediationComplexity > 0
                    ? g.ImpactScore / g.RemediationComplexity
                    : g.ImpactScore)
                .Take(request.TopPriorityCount)
                .Select(g => new CompliancePriorityItem(
                    GapId: g.GapId,
                    GapName: g.GapName,
                    ImpactScore: g.ImpactScore,
                    RemediationComplexity: g.RemediationComplexity,
                    PriorityScore: g.RemediationComplexity > 0
                        ? g.ImpactScore / g.RemediationComplexity
                        : g.ImpactScore,
                    AffectedStandards: g.AffectedStandards))
                .ToList();

            var matrix = crossGaps
                .SelectMany(g => standards.Select(s => new GapMatrixCell(
                    GapId: g.GapId,
                    Standard: s,
                    IsAffected: g.AffectedStandards.Contains(s, StringComparer.OrdinalIgnoreCase))))
                .ToList();

            int remediatedGaps = gaps.Count(g => g.IsRemediated);
            decimal tenantScore = ComputeTenantScore(gaps);
            var tier = ClassifyTier(crossGaps.Count(g => !g.IsRemediated));

            decimal totalImpact = crossGaps.Sum(g => g.ImpactScore);
            decimal top10Impact = priorityList.Sum(p => p.ImpactScore);
            decimal lift = totalImpact > 0 ? top10Impact / totalImpact * 100m : 0m;

            return Result<Report>.Success(new Report(
                GeneratedAt: now,
                TenantId: request.TenantId,
                Standards: standards,
                LookbackDays: request.LookbackDays,
                TenantCrossComplianceScore: tenantScore,
                Tier: tier,
                TotalGaps: gaps.Count,
                RemediatedGaps: remediatedGaps,
                CrossStandardGapsCount: crossGaps.Count,
                EstimatedComplianceLift: Math.Round(lift, 1),
                CrossStandardGaps: crossGaps,
                TenantCompliancePriorityList: priorityList,
                CrossStandardGapMatrix: matrix));
        }

        private static decimal ComputeImpactScore(ICrossStandardComplianceGapReader.ComplianceGapEntry g)
        {
            decimal tierWeight = g.ServiceTier switch
            {
                "Critical" => CriticalTierWeight,
                "Standard" => StandardTierWeight,
                _ => InternalTierWeight
            };
            return g.AffectedStandards.Count * tierWeight;
        }

        private static decimal ComputeTenantScore(
            IReadOnlyList<ICrossStandardComplianceGapReader.ComplianceGapEntry> gaps)
        {
            if (gaps.Count == 0) return 100m;
            decimal totalWeight = gaps.Sum(g => (decimal)Math.Max(g.AffectedStandards.Count, 1));
            decimal remediatedWeight = gaps
                .Where(g => g.IsRemediated)
                .Sum(g => (decimal)Math.Max(g.AffectedStandards.Count, 1));
            return totalWeight > 0 ? Math.Round(remediatedWeight / totalWeight * 100m, 1) : 100m;
        }

        private static CrossStandardGapTier ClassifyTier(int openCrossGaps) => openCrossGaps switch
        {
            _ when openCrossGaps <= MinimalThreshold => CrossStandardGapTier.Minimal,
            _ when openCrossGaps <= PartialThreshold => CrossStandardGapTier.Partial,
            _ when openCrossGaps <= SignificantThreshold => CrossStandardGapTier.Significant,
            _ => CrossStandardGapTier.Critical
        };

        private static Report EmptyReport(
            DateTimeOffset now, string tenantId, IReadOnlyList<string> standards, int lookbackDays)
            => new(
                GeneratedAt: now,
                TenantId: tenantId,
                Standards: standards,
                LookbackDays: lookbackDays,
                TenantCrossComplianceScore: 100m,
                Tier: CrossStandardGapTier.Minimal,
                TotalGaps: 0,
                RemediatedGaps: 0,
                CrossStandardGapsCount: 0,
                EstimatedComplianceLift: 0m,
                CrossStandardGaps: [],
                TenantCompliancePriorityList: [],
                CrossStandardGapMatrix: []);
    }
}
