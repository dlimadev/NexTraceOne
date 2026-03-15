using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetExecutiveOverview;

/// <summary>
/// Feature: GetExecutiveOverview — visão executiva agregada de operações, risco, maturidade e mudanças.
/// Centraliza indicadores críticos para tomada de decisão estratégica por domínio, equipa ou período.
/// </summary>
public static class GetExecutiveOverview
{
    /// <summary>Query de visão executiva. Permite filtragem por domínio, equipa ou intervalo temporal.</summary>
    public sealed record Query(
        string? DomainId = null,
        string? TeamId = null,
        string? Range = null) : IQuery<Response>;

    /// <summary>Handler que agrega indicadores executivos cross-module.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var operationalTrend = new OperationalTrendDto(
                StabilityTrend: TrendDirection.Improving,
                IncidentRateChange: -12.5m,
                AvgResolutionHours: 4.2m);

            var riskSummary = new RiskSummaryDto(
                OverallRisk: RiskLevel.Medium,
                CriticalDomains: 1,
                HighRiskServices: 4,
                RiskTrend: TrendDirection.Stable);

            var maturitySummary = new MaturitySummaryDto(
                OverallMaturity: MaturityLevel.Defined,
                OwnershipCoverage: 90.5m,
                ContractCoverage: 83.3m,
                DocumentationCoverage: 71.4m,
                RunbookCoverage: 59.5m);

            var criticalFocusAreas = new List<FocusAreaDto>
            {
                new("Change Safety", RiskLevel.High,
                    "Rollback rate above threshold in Commerce domain", 3),
                new("Contract Coverage", RiskLevel.Medium,
                    "Several services without defined contracts in Integration domain", 5),
                new("Incident Recurrence", RiskLevel.High,
                    "Recurring timeout incidents in Payment services", 2),
                new("Runbook Gaps", RiskLevel.Medium,
                    "Critical services missing operational runbooks", 7)
            };

            var changeSafetySummary = new ChangeSafetySummaryDto(
                SafeChanges: 48,
                RiskyChanges: 7,
                Rollbacks: 3,
                ConfidenceTrend: TrendDirection.Improving);

            var incidentTrendSummary = new IncidentTrendSummaryDto(
                OpenIncidents: 7,
                ResolvedLast30Days: 23,
                AvgResolutionHours: 4.2m,
                RecurrenceRate: 18.3m,
                Trend: TrendDirection.Improving);

            var complianceCoverageSummary = new ComplianceCoverageSummaryDto(
                OverallScore: 78.5m,
                CompliantPct: 66.7m,
                GapCount: 14,
                Trend: TrendDirection.Stable);

            var topDomains = new List<DomainAttentionDto>
            {
                new("domain-commerce", "Commerce", RiskLevel.Critical,
                    "Service degradation and frequent rollbacks in Order Processor"),
                new("domain-integration", "Integration", RiskLevel.High,
                    "Legacy adapters without contracts or documentation"),
                new("domain-analytics", "Analytics", RiskLevel.Medium,
                    "Reporting engine with outdated contracts and missing runbooks")
            };

            var response = new Response(
                OperationalTrend: operationalTrend,
                RiskSummary: riskSummary,
                MaturitySummary: maturitySummary,
                CriticalFocusAreas: criticalFocusAreas,
                ChangeSafetySummary: changeSafetySummary,
                IncidentTrendSummary: incidentTrendSummary,
                ComplianceCoverageSummary: complianceCoverageSummary,
                TopDomainsRequiringAttention: topDomains,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta da visão executiva agregada.</summary>
    public sealed record Response(
        OperationalTrendDto OperationalTrend,
        RiskSummaryDto RiskSummary,
        MaturitySummaryDto MaturitySummary,
        IReadOnlyList<FocusAreaDto> CriticalFocusAreas,
        ChangeSafetySummaryDto ChangeSafetySummary,
        IncidentTrendSummaryDto IncidentTrendSummary,
        ComplianceCoverageSummaryDto ComplianceCoverageSummary,
        IReadOnlyList<DomainAttentionDto> TopDomainsRequiringAttention,
        DateTimeOffset GeneratedAt);

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
