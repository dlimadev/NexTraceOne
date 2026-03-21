using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetTeamFinOps;

/// <summary>
/// Feature: GetTeamFinOps — perfil de custo contextual agregado por equipa.
/// Inclui resumo de custo, serviços, desperdício, eficiência e correlação com confiabilidade.
/// IMPLEMENTATION STATUS: Demo — returns illustrative data.
/// </summary>
public static class GetTeamFinOps
{
    /// <summary>Query para obter perfil de FinOps de uma equipa.</summary>
    public sealed record Query(string TeamId) : IQuery<Response>;

    /// <summary>Handler que retorna perfil de FinOps da equipa.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var services = new List<TeamServiceCostDto>
            {
                new("svc-order-processor", "Order Processor", CostEfficiency.Wasteful, 18700m, TrendDirection.Declining, 7500m, 58.3m),
                new("svc-inventory-sync", "Inventory Sync", CostEfficiency.Inefficient, 8900m, TrendDirection.Declining, 2800m, 81.0m),
                new("svc-catalog-sync", "Catalog Sync", CostEfficiency.Wasteful, 15200m, TrendDirection.Declining, 6700m, 65.4m)
            };

            var trendPoints = new List<TrendPointDto>
            {
                new("2025-10", 38200m), new("2025-11", 39800m), new("2025-12", 40500m),
                new("2026-01", 41200m), new("2026-02", 42100m), new("2026-03", 42800m)
            };

            var response = new Response(
                TeamId: request.TeamId,
                TeamName: "Team Commerce",
                Domain: "Commerce",
                TotalMonthlyCost: services.Sum(s => s.MonthlyCost),
                PreviousMonthCost: 40500m,
                CostTrend: TrendDirection.Declining,
                OverallEfficiency: CostEfficiency.Inefficient,
                TotalWaste: services.Sum(s => s.WasteAmount),
                ServiceCount: services.Count,
                Services: services,
                TrendSeries: trendPoints,
                AvgReliabilityScore: services.Average(s => s.ReliabilityScore),
                TotalRecentIncidents: 11,
                TopOptimizationFocus: "Reduce reprocessing waste in Order Processor and Catalog Sync",
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: true,
                DataSource: "demo");

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Perfil de FinOps agregado por equipa. IsSimulated=true indica dados demonstrativos.</summary>
    public sealed record Response(
        string TeamId,
        string TeamName,
        string Domain,
        decimal TotalMonthlyCost,
        decimal PreviousMonthCost,
        TrendDirection CostTrend,
        CostEfficiency OverallEfficiency,
        decimal TotalWaste,
        int ServiceCount,
        IReadOnlyList<TeamServiceCostDto> Services,
        IReadOnlyList<TrendPointDto> TrendSeries,
        decimal AvgReliabilityScore,
        int TotalRecentIncidents,
        string TopOptimizationFocus,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>Custo de serviço dentro da equipa.</summary>
    public sealed record TeamServiceCostDto(
        string ServiceId,
        string ServiceName,
        CostEfficiency Efficiency,
        decimal MonthlyCost,
        TrendDirection Trend,
        decimal WasteAmount,
        decimal ReliabilityScore);

    /// <summary>Ponto de série temporal de custo.</summary>
    public sealed record TrendPointDto(
        string Period,
        decimal Cost);
}
