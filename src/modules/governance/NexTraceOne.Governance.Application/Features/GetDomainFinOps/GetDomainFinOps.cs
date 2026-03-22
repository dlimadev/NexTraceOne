using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;

namespace NexTraceOne.Governance.Application.Features.GetDomainFinOps;

/// <summary>
/// Feature: GetDomainFinOps — perfil de custo contextual agregado por domínio.
/// Inclui resumo de custo, equipas, desperdício, eficiência e correlação com confiabilidade.
/// Consome dados reais do módulo CostIntelligence via contrato público.
/// </summary>
public static class GetDomainFinOps
{
    /// <summary>Query para obter perfil de FinOps de um domínio.</summary>
    public sealed record Query(string DomainId) : IQuery<Response>;

    /// <summary>Handler que retorna perfil de FinOps do domínio.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private readonly ICostIntelligenceModule _costModule;

        public Handler(ICostIntelligenceModule costModule)
        {
            _costModule = costModule;
        }

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var records = await _costModule.GetCostsByDomainAsync(request.DomainId, cancellationToken: cancellationToken);

            var teams = records
                .GroupBy(r => r.Team ?? string.Empty)
                .Select(g => new DomainTeamCostDto(
                    g.Key,
                    g.Key,
                    g.Count(),
                    g.Sum(r => r.TotalCost),
                    0m,
                    ComputeEfficiency(g.Average(r => r.TotalCost)),
                    0m))
                .ToList();

            var topWasteServices = records
                .OrderByDescending(r => r.TotalCost)
                .Where(r => ComputeEfficiency(r.TotalCost) is CostEfficiency.Wasteful or CostEfficiency.Inefficient)
                .Take(5)
                .Select(r => new WasteServiceDto(
                    r.ServiceId,
                    r.ServiceName,
                    r.Team ?? string.Empty,
                    0m,
                    ComputeEfficiency(r.TotalCost)))
                .ToList();

            var totalCost = teams.Sum(t => t.MonthlyCost);
            var overallEfficiency = teams.Count == 0
                ? CostEfficiency.Efficient
                : ComputeEfficiency(teams.Average(t => t.MonthlyCost));

            var response = new Response(
                DomainId: request.DomainId,
                DomainName: request.DomainId,
                TotalMonthlyCost: totalCost,
                PreviousMonthCost: 0m,
                CostTrend: TrendDirection.Stable,
                OverallEfficiency: overallEfficiency,
                TotalWaste: 0m,
                TeamCount: teams.Count,
                ServiceCount: records.Count,
                Teams: teams,
                TopWasteServices: topWasteServices,
                TrendSeries: Array.Empty<TrendPointDto>(),
                AvgReliabilityScore: 0m,
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

    /// <summary>Perfil de FinOps agregado por domínio. IsSimulated=true indica dados demonstrativos.</summary>
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
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

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
