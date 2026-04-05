using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.OperationalIntelligence.Contracts.Cost.ServiceInterfaces;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetFinOpsSummary;

/// <summary>
/// Feature: GetFinOpsSummary — resumo de FinOps contextual por serviço, equipa e domínio.
/// FinOps no NexTraceOne é contextual: ligado a operação, comportamento e eficiência.
/// Consome dados reais do módulo CostIntelligence via contrato público.
/// </summary>
public static class GetFinOpsSummary
{
    /// <summary>Query de resumo de FinOps contextual.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null,
        string? ServiceId = null,
        string? Range = null) : IQuery<Response>;

    /// <summary>Handler que computa resumo de FinOps contextual.</summary>
    /// <summary>Valida os filtros opcionais da query de resumo FinOps.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).MaximumLength(200)
                .When(x => x.TeamId is not null);
            RuleFor(x => x.DomainId).MaximumLength(200)
                .When(x => x.DomainId is not null);
            RuleFor(x => x.ServiceId).MaximumLength(200)
                .When(x => x.ServiceId is not null);
            RuleFor(x => x.Range).MaximumLength(200)
                .When(x => x.Range is not null);
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
            var records = await _costModule.GetCostRecordsAsync(request.Range, cancellationToken) ?? [];

            var filtered = records.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(r => (r.Team ?? string.Empty).Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.DomainId))
                filtered = filtered.Where(r => (r.Domain ?? string.Empty).Equals(request.DomainId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(r => r.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));

            var costRecords = filtered.ToList();

            var services = costRecords
                .Select(r => new ServiceCostDto(
                    r.ServiceId,
                    r.ServiceName,
                    r.Domain ?? string.Empty,
                    r.Team ?? string.Empty,
                    ComputeEfficiency(r.TotalCost),
                    r.TotalCost,
                    TrendDirection.Stable,
                    BuildWasteSignals(r.TotalCost, costRecords),
                    null))
                .ToList();

            var topDrivers = services
                .OrderByDescending(s => s.MonthlyCost)
                .Take(3)
                .Select(s => new CostDriverDto(s.ServiceId, s.ServiceName, s.MonthlyCost, s.Efficiency))
                .ToList();

            var optimizationOpportunities = services
                .Where(s => s.Efficiency is CostEfficiency.Wasteful or CostEfficiency.Inefficient)
                .Select(s => new OptimizationOpportunityDto(
                    s.ServiceId, s.ServiceName,
                    0m,
                    s.Efficiency == CostEfficiency.Wasteful ? "High" : "Medium",
                    $"Review cost allocation for {s.ServiceName}"))
                .ToList();

            var overallEfficiency = services.Count == 0
                ? CostEfficiency.Efficient
                : ComputeEfficiency(services.Average(s => s.MonthlyCost));

            var response = new Response(
                TotalMonthlyCost: services.Sum(s => s.MonthlyCost),
                TotalWaste: services.Sum(s => s.WasteSignals.Sum(w => w.EstimatedWaste)),
                OverallEfficiency: overallEfficiency,
                CostTrend: TrendDirection.Stable,
                Services: services,
                TopCostDrivers: topDrivers,
                TopWasteSignals: services
                    .SelectMany(s => s.WasteSignals)
                    .OrderByDescending(s => s.EstimatedWaste)
                    .Take(5)
                    .ToList(),
                OptimizationOpportunities: optimizationOpportunities,
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

        private static IReadOnlyList<WasteSignalDto> BuildWasteSignals(decimal currentCost, IReadOnlyList<CostRecordSummary> allRecords)
        {
            if (allRecords.Count == 0) return Array.Empty<WasteSignalDto>();
            var average = allRecords.Average(r => r.TotalCost);
            var waste = Math.Max(0m, currentCost - average);
            if (waste <= 0m) return Array.Empty<WasteSignalDto>();

            return
            [
                new WasteSignalDto(
                    Description: "Cost is above tenant average for the selected scope.",
                    Pattern: "cost-above-average",
                    Type: WasteSignalType.DegradedCostAmplification,
                    EstimatedWaste: Math.Round(waste, 2))
            ];
        }
    }

    /// <summary>Resposta do resumo de FinOps. IsSimulated=true indica dados demonstrativos.</summary>
    public sealed record Response(
        decimal TotalMonthlyCost,
        decimal TotalWaste,
        CostEfficiency OverallEfficiency,
        TrendDirection CostTrend,
        IReadOnlyList<ServiceCostDto> Services,
        IReadOnlyList<CostDriverDto> TopCostDrivers,
        IReadOnlyList<WasteSignalDto> TopWasteSignals,
        IReadOnlyList<OptimizationOpportunityDto> OptimizationOpportunities,
        DateTimeOffset GeneratedAt,
        bool IsSimulated = false,
        string? DataSource = null);

    /// <summary>Custo contextual por serviço.</summary>
    public sealed record ServiceCostDto(
        string ServiceId,
        string ServiceName,
        string Domain,
        string Team,
        CostEfficiency Efficiency,
        decimal MonthlyCost,
        TrendDirection Trend,
        IReadOnlyList<WasteSignalDto> WasteSignals,
        ReliabilityCorrelationDto? ReliabilityCorrelation);

    /// <summary>Sinal de desperdício operacional identificado.</summary>
    public sealed record WasteSignalDto(
        string Description,
        string Pattern,
        WasteSignalType Type,
        decimal EstimatedWaste);

    /// <summary>Correlação de custo com confiabilidade operacional.</summary>
    public sealed record ReliabilityCorrelationDto(
        decimal ReliabilityScore,
        int RecentIncidents,
        TrendDirection ReliabilityTrend);

    /// <summary>Principal driver de custo.</summary>
    public sealed record CostDriverDto(
        string ServiceId,
        string ServiceName,
        decimal MonthlyCost,
        CostEfficiency Efficiency);

    /// <summary>Oportunidade de otimização de custo.</summary>
    public sealed record OptimizationOpportunityDto(
        string ServiceId,
        string ServiceName,
        decimal PotentialSavings,
        string Priority,
        string Recommendation);
}
