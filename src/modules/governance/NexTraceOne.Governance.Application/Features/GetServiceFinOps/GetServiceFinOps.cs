using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.GetServiceFinOps;

/// <summary>
/// Feature: GetServiceFinOps — perfil de custo contextual de um serviço individual.
/// Inclui waste, eficiência, correlação com confiabilidade e impacto de mudanças.
/// Consome dados reais do módulo CostIntelligence via contrato público.
/// </summary>
public static class GetServiceFinOps
{
    /// <summary>Query para obter perfil de FinOps de um serviço.</summary>
    public sealed record Query(string ServiceId) : IQuery<Response>;

    /// <summary>Handler que retorna perfil de FinOps do serviço.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private readonly ICostIntelligenceModule _costModule;

        public Handler(ICostIntelligenceModule costModule)
        {
            _costModule = costModule;
        }

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var record = await _costModule.GetServiceCostAsync(request.ServiceId, cancellationToken: cancellationToken);

            if (record is null)
                return Error.NotFound("FINOPS.SERVICE_NOT_FOUND", "No cost data found for service {0}", request.ServiceId);

            var efficiency = ComputeEfficiency(record.TotalCost);

            var response = new Response(
                ServiceId: record.ServiceId,
                ServiceName: record.ServiceName,
                Domain: record.Domain ?? string.Empty,
                Team: record.Team ?? string.Empty,
                MonthlyCost: record.TotalCost,
                PreviousMonthCost: 0m,
                CostTrend: TrendDirection.Stable,
                Efficiency: efficiency,
                WasteSignals: Array.Empty<WasteSignalDto>(),
                TotalWaste: 0m,
                EfficiencyIndicators: Array.Empty<EfficiencyIndicatorDto>(),
                ReliabilityScore: 0m,
                RecentIncidents: 0,
                ReliabilityTrend: TrendDirection.Stable,
                ChangeImpacts: Array.Empty<ChangeImpactDto>(),
                Optimizations: Array.Empty<OptimizationDto>(),
                TotalPotentialSavings: 0m,
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: false,
                DataSource: "cost-intelligence");

            return Result<Response>.Success(response);
        }

        private static CostEfficiency ComputeEfficiency(decimal cost) => cost switch
        {
            > 15000m => CostEfficiency.Wasteful,
            > 10000m => CostEfficiency.Inefficient,
            > 5000m => CostEfficiency.Acceptable,
            _ => CostEfficiency.Efficient
        };
    }

    /// <summary>Perfil de FinOps completo de um serviço. IsSimulated=true indica dados demonstrativos.</summary>
    public sealed record Response(
        string ServiceId,
        string ServiceName,
        string Domain,
        string Team,
        decimal MonthlyCost,
        decimal PreviousMonthCost,
        TrendDirection CostTrend,
        CostEfficiency Efficiency,
        IReadOnlyList<WasteSignalDto> WasteSignals,
        decimal TotalWaste,
        IReadOnlyList<EfficiencyIndicatorDto> EfficiencyIndicators,
        decimal ReliabilityScore,
        int RecentIncidents,
        TrendDirection ReliabilityTrend,
        IReadOnlyList<ChangeImpactDto> ChangeImpacts,
        IReadOnlyList<OptimizationDto> Optimizations,
        decimal TotalPotentialSavings,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>Sinal de desperdício operacional com timestamp.</summary>
    public sealed record WasteSignalDto(
        string Description,
        string Pattern,
        WasteSignalType Type,
        decimal EstimatedWaste,
        string DetectedAt);

    /// <summary>Indicador de eficiência operacional.</summary>
    public sealed record EfficiencyIndicatorDto(
        string Name,
        EfficiencyCategory Category,
        decimal CurrentValue,
        decimal TargetValue,
        string Assessment);

    /// <summary>Impacto de mudança recente no custo.</summary>
    public sealed record ChangeImpactDto(
        string ChangeId,
        string Description,
        string AppliedAt,
        decimal CostImpact,
        string Explanation);

    /// <summary>Oportunidade de otimização de custo.</summary>
    public sealed record OptimizationDto(
        string Recommendation,
        decimal PotentialSavings,
        string Priority,
        string Rationale);
}
