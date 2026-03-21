using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetExecutiveOverview;

/// <summary>
/// Feature: GetExecutiveOverview — visão executiva agregada de governança enterprise.
/// Compliance, risco e foco de atenção derivados de dados reais de Packs, Rollouts e Waivers.
/// Métricas cross-module (incidentes, mudanças) não estão disponíveis neste módulo.
/// </summary>
public static class GetExecutiveOverview
{
    /// <summary>Query de visão executiva. Permite filtragem por domínio, equipa ou intervalo temporal.</summary>
    public sealed record Query(
        string? DomainId = null,
        string? TeamId = null,
        string? Range = null) : IQuery<Response>;

    /// <summary>
    /// Handler que agrega indicadores executivos a partir de dados reais de Governance Packs,
    /// Rollouts e Waivers. Métricas de incidentes e mudanças são cross-module e retornam valores neutros.
    /// </summary>
    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernanceWaiverRepository waiverRepository,
        IGovernanceRolloutRecordRepository rolloutRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var packs = await packRepository.ListAsync(category: null, status: null, ct: cancellationToken);
            var waivers = await waiverRepository.ListAsync(packId: null, status: null, ct: cancellationToken);
            var rollouts = await rolloutRepository.ListAsync(
                packId: null, scopeType: null, scopeValue: null, status: null, ct: cancellationToken);

            var totalPacks = packs.Count;
            var pendingWaivers = waivers.Count(w => w.Status == WaiverStatus.Pending);
            var approvedWaivers = waivers.Count(w => w.Status == WaiverStatus.Approved);
            var failedRollouts = rollouts.Count(r => r.Status == RolloutStatus.Failed);
            var completedRollouts = rollouts.Count(r => r.Status == RolloutStatus.Completed);
            var pendingRollouts = rollouts.Count(r => r.Status == RolloutStatus.Pending);

            // Compliance score real
            var nonCompliantPacks = packs.Count(p =>
                waivers.Any(w => w.PackId == p.Id && w.Status == WaiverStatus.Pending));
            var complianceScore = totalPacks == 0 ? 0m : Math.Round(((decimal)(totalPacks - nonCompliantPacks) / totalPacks) * 100m, 1);
            var compliantPct = complianceScore;
            var gapCount = nonCompliantPacks + packs.Count(p => p.Status == GovernancePackStatus.Draft);

            // Risk derivado de rollouts/waivers
            var overallRisk = failedRollouts > 0
                ? RiskLevel.Critical
                : pendingWaivers > 2
                    ? RiskLevel.High
                    : pendingWaivers > 0 || pendingRollouts > 0
                        ? RiskLevel.Medium
                        : RiskLevel.Low;

            var riskTrend = failedRollouts == 0 && completedRollouts > 0
                ? TrendDirection.Improving
                : failedRollouts > 0
                    ? TrendDirection.Declining
                    : TrendDirection.Stable;

            var complianceTrend = nonCompliantPacks == 0
                ? TrendDirection.Improving
                : nonCompliantPacks > totalPacks / 2
                    ? TrendDirection.Declining
                    : TrendDirection.Stable;

            // Maturidade global derivada da taxa de rollouts
            var rolloutRate = rollouts.Count == 0 ? 0m : (decimal)completedRollouts / rollouts.Count;
            var overallMaturity = rolloutRate >= 0.9m ? MaturityLevel.Optimizing
                : rolloutRate >= 0.7m ? MaturityLevel.Managed
                : rolloutRate >= 0.5m ? MaturityLevel.Defined
                : rolloutRate >= 0.2m ? MaturityLevel.Developing
                : MaturityLevel.Initial;

            // Áreas críticas de foco: baseadas em packs com problemas reais
            var focusAreas = BuildFocusAreas(packs, waivers, rollouts);

            // Domínios com atenção: derivados de packs com waivers pendentes ou rollouts falhados
            var topDomains = BuildTopDomains(packs, waivers, rollouts);

            // Tendência de confiança em mudanças baseada nos rollouts
            var changeTrend = failedRollouts == 0 && completedRollouts > 0
                ? TrendDirection.Improving
                : failedRollouts > 0 ? TrendDirection.Declining : TrendDirection.Stable;

            var operationalTrend = new OperationalTrendDto(
                StabilityTrend: riskTrend,
                IncidentRateChange: 0m,    // cross-module — não disponível neste módulo
                AvgResolutionHours: 0m);   // cross-module — não disponível neste módulo

            var riskSummary = new RiskSummaryDto(
                OverallRisk: overallRisk,
                CriticalDomains: packs.Count(p => p.Status == GovernancePackStatus.Draft),
                HighRiskServices: packs.Count(p =>
                    rollouts.Any(r => r.PackId == p.Id && r.Status == RolloutStatus.Failed)),
                RiskTrend: riskTrend);

            var maturitySummary = new MaturitySummaryDto(
                OverallMaturity: overallMaturity,
                OwnershipCoverage: ComputeCategoryRolloutRate(packs, rollouts, GovernanceRuleCategory.Reliability),
                ContractCoverage: ComputeCategoryRolloutRate(packs, rollouts, GovernanceRuleCategory.Contracts),
                DocumentationCoverage: ComputeCategoryRolloutRate(packs, rollouts, GovernanceRuleCategory.SourceOfTruth),
                RunbookCoverage: ComputeCategoryRolloutRate(packs, rollouts, GovernanceRuleCategory.Operations));

            var changeSafetySummary = new ChangeSafetySummaryDto(
                SafeChanges: completedRollouts,
                RiskyChanges: pendingRollouts + failedRollouts,
                Rollbacks: failedRollouts,
                ConfidenceTrend: changeTrend);

            var incidentTrendSummary = new IncidentTrendSummaryDto(
                OpenIncidents: 0,        // cross-module — não disponível neste módulo
                ResolvedLast30Days: 0,   // cross-module — não disponível neste módulo
                AvgResolutionHours: 0m,  // cross-module — não disponível neste módulo
                RecurrenceRate: 0m,      // cross-module — não disponível neste módulo
                Trend: TrendDirection.Stable);

            var complianceCoverageSummary = new ComplianceCoverageSummaryDto(
                OverallScore: complianceScore,
                CompliantPct: compliantPct,
                GapCount: gapCount,
                Trend: complianceTrend);

            var response = new Response(
                OperationalTrend: operationalTrend,
                RiskSummary: riskSummary,
                MaturitySummary: maturitySummary,
                CriticalFocusAreas: focusAreas,
                ChangeSafetySummary: changeSafetySummary,
                IncidentTrendSummary: incidentTrendSummary,
                ComplianceCoverageSummary: complianceCoverageSummary,
                TopDomainsRequiringAttention: topDomains,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Result<Response>.Success(response);
        }

        private static decimal ComputeCategoryRolloutRate(
            IReadOnlyList<GovernancePack> packs,
            IReadOnlyList<GovernanceRolloutRecord> rollouts,
            GovernanceRuleCategory category)
        {
            var categoryPacks = packs.Where(p => p.Category == category).ToList();
            if (categoryPacks.Count == 0) return 0m;
            var completedForCategory = rollouts.Count(r =>
                r.Status == RolloutStatus.Completed &&
                categoryPacks.Any(p => p.Id == r.PackId));
            return Math.Round(((decimal)completedForCategory / categoryPacks.Count) * 100m, 1);
        }

        private static IReadOnlyList<FocusAreaDto> BuildFocusAreas(
            IReadOnlyList<GovernancePack> packs,
            IReadOnlyList<GovernanceWaiver> waivers,
            IReadOnlyList<GovernanceRolloutRecord> rollouts)
        {
            var areas = new List<FocusAreaDto>();

            var failedPacks = packs.Where(p => rollouts.Any(r => r.PackId == p.Id && r.Status == RolloutStatus.Failed)).ToList();
            if (failedPacks.Count > 0)
                areas.Add(new FocusAreaDto("Failed Rollouts", RiskLevel.Critical,
                    $"{failedPacks.Count} governance pack(s) have failed rollouts requiring remediation", failedPacks.Count));

            var pendingWaiverPacks = packs.Where(p => waivers.Any(w => w.PackId == p.Id && w.Status == WaiverStatus.Pending)).ToList();
            if (pendingWaiverPacks.Count > 0)
                areas.Add(new FocusAreaDto("Pending Waivers", RiskLevel.High,
                    $"{pendingWaiverPacks.Count} governance pack(s) have pending waivers awaiting approval", pendingWaiverPacks.Count));

            var draftPacks = packs.Where(p => p.Status == GovernancePackStatus.Draft).ToList();
            if (draftPacks.Count > 0)
                areas.Add(new FocusAreaDto("Draft Packs", RiskLevel.Medium,
                    $"{draftPacks.Count} governance pack(s) are still in draft and not yet published", draftPacks.Count));

            var pendingRolloutPacks = packs.Where(p => rollouts.Any(r => r.PackId == p.Id && r.Status == RolloutStatus.Pending)).ToList();
            if (pendingRolloutPacks.Count > 0)
                areas.Add(new FocusAreaDto("Pending Rollouts", RiskLevel.Low,
                    $"{pendingRolloutPacks.Count} governance pack(s) have pending rollouts in progress", pendingRolloutPacks.Count));

            return areas;
        }

        private static IReadOnlyList<DomainAttentionDto> BuildTopDomains(
            IReadOnlyList<GovernancePack> packs,
            IReadOnlyList<GovernanceWaiver> waivers,
            IReadOnlyList<GovernanceRolloutRecord> rollouts)
        {
            // Agrega por categoria de pack (proxy de domínio até integração cross-module)
            var categoryRisks = packs
                .GroupBy(p => p.Category)
                .Select(g =>
                {
                    var categoryPacks = g.ToList();
                    var failedRollouts = rollouts.Count(r => r.Status == RolloutStatus.Failed && categoryPacks.Any(p => p.Id == r.PackId));
                    var pendingWaivers = waivers.Count(w => w.Status == WaiverStatus.Pending && categoryPacks.Any(p => p.Id == w.PackId));

                    var risk = failedRollouts > 0 ? RiskLevel.Critical
                        : pendingWaivers > 0 ? RiskLevel.High
                        : categoryPacks.Any(p => p.Status == GovernancePackStatus.Draft) ? RiskLevel.Medium
                        : RiskLevel.Low;

                    var reason = failedRollouts > 0
                        ? $"{failedRollouts} failed rollout(s) in {g.Key} governance packs"
                        : pendingWaivers > 0
                            ? $"{pendingWaivers} pending waiver(s) for {g.Key} governance packs"
                            : $"{g.Key} governance packs have pending activities";

                    return new DomainAttentionDto(
                        DomainId: g.Key.ToString().ToLowerInvariant(),
                        DomainName: g.Key.ToString(),
                        RiskLevel: risk,
                        Reason: reason);
                })
                .Where(d => d.RiskLevel != RiskLevel.Low)
                .OrderByDescending(d => d.RiskLevel)
                .Take(5)
                .ToList();

            return categoryRisks;
        }
    }

    /// <summary>
    /// Resposta da visão executiva agregada.
    /// CrossModuleDataAvailable=false indica que métricas de incidentes e mudanças cross-module
    /// retornam valores neutros (0) porque dependem de integração futura entre módulos.
    /// </summary>
    public sealed record Response(
        OperationalTrendDto OperationalTrend,
        RiskSummaryDto RiskSummary,
        MaturitySummaryDto MaturitySummary,
        IReadOnlyList<FocusAreaDto> CriticalFocusAreas,
        ChangeSafetySummaryDto ChangeSafetySummary,
        IncidentTrendSummaryDto IncidentTrendSummary,
        ComplianceCoverageSummaryDto ComplianceCoverageSummary,
        IReadOnlyList<DomainAttentionDto> TopDomainsRequiringAttention,
        DateTimeOffset GeneratedAt,
        bool CrossModuleDataAvailable = false);

    /// <summary>Tendência operacional com estabilidade e resolução de incidentes.</summary>
    public sealed record OperationalTrendDto(
        TrendDirection StabilityTrend,
        decimal IncidentRateChange,
        decimal AvgResolutionHours);

    /// <summary>Resumo de risco com domínios críticos e tendência.</summary>
    public sealed record RiskSummaryDto(
        RiskLevel OverallRisk,
        int CriticalDomains,
        int HighRiskServices,
        TrendDirection RiskTrend);

    /// <summary>Resumo de maturidade com cobertura por dimensão.</summary>
    public sealed record MaturitySummaryDto(
        MaturityLevel OverallMaturity,
        decimal OwnershipCoverage,
        decimal ContractCoverage,
        decimal DocumentationCoverage,
        decimal RunbookCoverage);

    /// <summary>Área de foco crítico que requer atenção executiva.</summary>
    public sealed record FocusAreaDto(
        string AreaName,
        RiskLevel Severity,
        string Description,
        int AffectedServices);

    /// <summary>Resumo de segurança de mudanças com rollbacks e tendência de confiança.</summary>
    public sealed record ChangeSafetySummaryDto(
        int SafeChanges,
        int RiskyChanges,
        int Rollbacks,
        TrendDirection ConfidenceTrend);

    /// <summary>Resumo de tendência de incidentes com recorrência e resolução.</summary>
    public sealed record IncidentTrendSummaryDto(
        int OpenIncidents,
        int ResolvedLast30Days,
        decimal AvgResolutionHours,
        decimal RecurrenceRate,
        TrendDirection Trend);

    /// <summary>Resumo de cobertura de compliance com gaps e tendência.</summary>
    public sealed record ComplianceCoverageSummaryDto(
        decimal OverallScore,
        decimal CompliantPct,
        int GapCount,
        TrendDirection Trend);

    /// <summary>Domínio que requer atenção executiva com nível de risco e justificação.</summary>
    public sealed record DomainAttentionDto(
        string DomainId,
        string DomainName,
        RiskLevel RiskLevel,
        string Reason);
}
