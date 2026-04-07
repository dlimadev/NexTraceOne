using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetTeamFinOps;

/// <summary>
/// Feature: GetTeamFinOps — perfil de custo contextual agregado por equipa.
/// Inclui resumo de custo, serviços, desperdício, eficiência e correlação com confiabilidade.
/// Consome dados reais do módulo CostIntelligence via contrato público.
/// </summary>
public static class GetTeamFinOps
{
    /// <summary>Query para obter perfil de FinOps de uma equipa.</summary>
    public sealed record Query(string TeamId) : IQuery<Response>;

    /// <summary>Handler que retorna perfil de FinOps da equipa.</summary>
    /// <summary>Valida os parâmetros da query de FinOps por equipa.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private readonly ICostIntelligenceModule _costModule;

        public Handler(ICostIntelligenceModule costModule)
        {
            _costModule = costModule;
        }

        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var records = await _costModule.GetCostsByTeamAsync(request.TeamId, cancellationToken: cancellationToken) ?? [];
            var allRecords = await _costModule.GetCostRecordsAsync(cancellationToken: cancellationToken) ?? [];

            var latestRecordsByService = records
                .GroupBy(r => r.ServiceId)
                .Select(g => g.OrderByDescending(r => r.Period, StringComparer.Ordinal).First())
                .ToList();

            var previousRecordsByService = records
                .GroupBy(r => r.ServiceId)
                .Select(g => g.OrderByDescending(r => r.Period, StringComparer.Ordinal).Skip(1).FirstOrDefault())
                .Where(r => r is not null)
                .Cast<CostRecordSummary>()
                .ToDictionary(r => r.ServiceId, StringComparer.OrdinalIgnoreCase);

            var teamAverageCost = latestRecordsByService.Count == 0 ? 0m : latestRecordsByService.Average(r => r.TotalCost);
            var tenantAverageCost = allRecords.Count == 0 ? teamAverageCost : allRecords.Average(r => r.TotalCost);

            var services = latestRecordsByService
                .Select(r => new TeamServiceCostDto(
                    r.ServiceId,
                    r.ServiceName,
                    ComputeEfficiency(r.TotalCost),
                    r.TotalCost,
                    GetTrendDirection(previousRecordsByService.GetValueOrDefault(r.ServiceId)?.TotalCost ?? 0m, r.TotalCost),
                    Math.Round(Math.Max(0m, r.TotalCost - tenantAverageCost), 2),
                    0m))
                .ToList();

            var totalCost = services.Sum(s => s.MonthlyCost);
            var previousMonthCost = previousRecordsByService.Values.Sum(v => v.TotalCost);
            var overallEfficiency = services.Count == 0
                ? CostEfficiency.Efficient
                : ComputeEfficiency(services.Average(s => s.MonthlyCost));

            var topFocus = services.Count == 0
                ? string.Empty
                : $"Review cost allocation for {services.OrderByDescending(s => s.MonthlyCost).First().ServiceName}";

            var response = new Response(
                TeamId: request.TeamId,
                TeamName: request.TeamId,
                Domain: records.FirstOrDefault()?.Domain ?? string.Empty,
                TotalMonthlyCost: totalCost,
                PreviousMonthCost: previousMonthCost,
                CostTrend: GetTrendDirection(previousMonthCost, totalCost),
                OverallEfficiency: overallEfficiency,
                TotalWaste: services.Sum(s => s.WasteAmount),
                ServiceCount: services.Count,
                Services: services,
                TrendSeries: BuildTeamTrendSeries(records),
                AvgReliabilityScore: 0m,
                TotalRecentIncidents: 0,
                TopOptimizationFocus: topFocus,
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

        private static TrendDirection GetTrendDirection(decimal previous, decimal current)
        {
            if (previous <= 0m) return TrendDirection.Stable;
            var deltaPercent = (current - previous) / previous * 100m;
            return deltaPercent switch
            {
                > 5m => TrendDirection.Declining,
                < -5m => TrendDirection.Improving,
                _ => TrendDirection.Stable
            };
        }

        private static IReadOnlyList<TrendPointDto> BuildTeamTrendSeries(IReadOnlyList<CostRecordSummary> records) =>
            records
                .GroupBy(r => r.Period)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new TrendPointDto(g.Key, g.Sum(r => r.TotalCost)))
                .ToList();
    }

    /// <summary>Perfil de FinOps agregado por equipa. IsSimulated é sempre false — dados reais via ICostIntelligenceModule.</summary>
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
