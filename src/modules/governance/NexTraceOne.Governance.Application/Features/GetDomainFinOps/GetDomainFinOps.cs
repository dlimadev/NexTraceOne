using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetDomainFinOps;

/// <summary>
/// Feature: GetDomainFinOps — perfil de custo contextual agregado por domínio.
/// Inclui resumo de custo, equipas, desperdício, eficiência e correlação com confiabilidade.
/// </summary>
public static class GetDomainFinOps
{
    /// <summary>Query para obter perfil de FinOps de um domínio.</summary>
    public sealed record Query(string DomainId) : IQuery<Response>;

    /// <summary>Handler que retorna perfil de FinOps do domínio.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var teams = new List<DomainTeamCostDto>
            {
                new("team-commerce", "Team Commerce", 3, 42800m, 17000m, CostEfficiency.Inefficient, 68.2m),
                new("team-platform", "Team Platform", 2, 22000m, 6700m, CostEfficiency.Acceptable, 82.5m)
            };

            var topWasteServices = new List<WasteServiceDto>
            {
                new("svc-order-processor", "Order Processor", "Team Commerce", 7500m, CostEfficiency.Wasteful),
                new("svc-catalog-sync", "Catalog Sync", "Team Platform", 6700m, CostEfficiency.Wasteful),
                new("svc-inventory-sync", "Inventory Sync", "Team Commerce", 2800m, CostEfficiency.Inefficient)
            };

            var trendPoints = new List<TrendPointDto>
            {
                new("2025-10", 58200m), new("2025-11", 60100m), new("2025-12", 61500m),
                new("2026-01", 62800m), new("2026-02", 63500m), new("2026-03", 64800m)
            };

            var response = new Response(
                DomainId: request.DomainId,
                DomainName: "Commerce",
                TotalMonthlyCost: teams.Sum(t => t.MonthlyCost),
                PreviousMonthCost: 63500m,
                CostTrend: TrendDirection.Declining,
                OverallEfficiency: CostEfficiency.Inefficient,
                TotalWaste: teams.Sum(t => t.WasteAmount),
                TeamCount: teams.Count,
                ServiceCount: teams.Sum(t => t.ServiceCount),
                Teams: teams,
                TopWasteServices: topWasteServices,
                TrendSeries: trendPoints,
                AvgReliabilityScore: teams.Average(t => t.AvgReliabilityScore),
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Perfil de FinOps agregado por domínio.</summary>
    public sealed record Response(
        string DomainId,
        string DomainName,
        decimal TotalMonthlyCost,
        decimal PreviousMonthCost,
        TrendDirection CostTrend,
        CostEfficiency OverallEfficiency,
        decimal TotalWaste,
        int TeamCount,
        int ServiceCount,
        IReadOnlyList<DomainTeamCostDto> Teams,
        IReadOnlyList<WasteServiceDto> TopWasteServices,
        IReadOnlyList<TrendPointDto> TrendSeries,
        decimal AvgReliabilityScore,
        DateTimeOffset GeneratedAt);

    /// <summary>Custo de equipa dentro do domínio.</summary>
    public sealed record DomainTeamCostDto(
        string TeamId,
        string TeamName,
        int ServiceCount,
        decimal MonthlyCost,
        decimal WasteAmount,
        CostEfficiency Efficiency,
        decimal AvgReliabilityScore);

    /// <summary>Serviço com maior desperdício no domínio.</summary>
    public sealed record WasteServiceDto(
        string ServiceId,
        string ServiceName,
        string Team,
        decimal WasteAmount,
        CostEfficiency Efficiency);

    /// <summary>Ponto de série temporal de custo.</summary>
    public sealed record TrendPointDto(
        string Period,
        decimal Cost);
}
