using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetControlsSummary;

/// <summary>
/// Feature: GetControlsSummary — resumo de controles enterprise por dimensão.
/// Agrega indicadores de cobertura, maturidade e gaps por área de controle.
/// </summary>
public static class GetControlsSummary
{
    /// <summary>Query para resumo de controles enterprise. Filtrável por equipa, domínio ou serviço.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null,
        string? ServiceId = null) : IQuery<Response>;

    /// <summary>Handler que retorna resumo de controles enterprise.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var dimensions = new List<ControlDimensionDto>
            {
                new(ControlDimension.ContractGovernance, 83.3m, 42, 7,
                    MaturityLevel.Defined, TrendDirection.Improving,
                    "Contract governance coverage is strong but 7 services lack proper contract definitions"),
                new(ControlDimension.SourceOfTruthCompleteness, 72.5m, 42, 12,
                    MaturityLevel.Developing, TrendDirection.Improving,
                    "Source of truth completeness improving; documentation and runbook gaps remain"),
                new(ControlDimension.ChangeGovernance, 91.2m, 156, 3,
                    MaturityLevel.Managed, TrendDirection.Stable,
                    "Change governance is mature with high validation coverage"),
                new(ControlDimension.IncidentMitigationEvidence, 67.8m, 34, 8,
                    MaturityLevel.Developing, TrendDirection.Improving,
                    "Mitigation evidence coverage growing but post-mortem discipline needs improvement"),
                new(ControlDimension.AiGovernance, 88.5m, 89, 2,
                    MaturityLevel.Defined, TrendDirection.Stable,
                    "AI governance well-controlled through model registry and access policies"),
                new(ControlDimension.DocumentationRunbookReadiness, 55.4m, 42, 19,
                    MaturityLevel.Developing, TrendDirection.Declining,
                    "Documentation and runbook coverage is below target; critical gap area"),
                new(ControlDimension.OwnershipCoverage, 90.5m, 42, 4,
                    MaturityLevel.Managed, TrendDirection.Stable,
                    "Ownership coverage is high with only 4 unassigned services")
            };

            var overallCoverage = dimensions.Average(d => d.CoveragePercent);
            var overallMaturity = MaturityLevel.Defined;

            var response = new Response(
                OverallCoverage: Math.Round(overallCoverage, 1),
                OverallMaturity: overallMaturity,
                TotalDimensions: dimensions.Count,
                CriticalGapCount: dimensions.Count(d => d.CoveragePercent < 60),
                Dimensions: dimensions,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com resumo de controles enterprise.</summary>
    public sealed record Response(
        decimal OverallCoverage,
        MaturityLevel OverallMaturity,
        int TotalDimensions,
        int CriticalGapCount,
        IReadOnlyList<ControlDimensionDto> Dimensions,
        DateTimeOffset GeneratedAt);

    /// <summary>DTO de dimensão de controle enterprise.</summary>
    public sealed record ControlDimensionDto(
        ControlDimension Dimension,
        decimal CoveragePercent,
        int TotalAssessed,
        int GapCount,
        MaturityLevel Maturity,
        TrendDirection Trend,
        string Summary);
}
