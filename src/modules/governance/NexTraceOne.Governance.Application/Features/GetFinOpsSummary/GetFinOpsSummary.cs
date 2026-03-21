using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetFinOpsSummary;

/// <summary>
/// Feature: GetFinOpsSummary — resumo de FinOps contextual por serviço, equipa e domínio.
/// FinOps no NexTraceOne é contextual: ligado a operação, comportamento e eficiência.
/// IMPLEMENTATION STATUS: Demo — returns illustrative data. Will be replaced by real
/// cost snapshot integration from the CostIntelligence submodule in a future sprint.
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
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var serviceIndicators = new List<ServiceCostDto>
            {
                new("svc-payment-api", "Payment API", "Payments", "Team Payments",
                    CostEfficiency.Inefficient, 12500m, TrendDirection.Declining,
                    new[] { new WasteSignalDto("Excessive retries on timeout", "retry-pattern", WasteSignalType.ExcessiveRetries, 3200m) },
                    new ReliabilityCorrelationDto(72.5m, 3, TrendDirection.Declining)),
                new("svc-order-processor", "Order Processor", "Commerce", "Team Commerce",
                    CostEfficiency.Wasteful, 18700m, TrendDirection.Declining,
                    new[] { new WasteSignalDto("Frequent rollbacks causing reprocessing", "rollback-waste", WasteSignalType.RepeatedReprocessing, 5400m),
                            new WasteSignalDto("Idle compute during off-peak", "idle-compute", WasteSignalType.IdleCostlyResource, 2100m) },
                    new ReliabilityCorrelationDto(58.3m, 5, TrendDirection.Declining)),
                new("svc-user-service", "User Service", "Identity", "Team Identity",
                    CostEfficiency.Acceptable, 4200m, TrendDirection.Stable,
                    Array.Empty<WasteSignalDto>(),
                    new ReliabilityCorrelationDto(95.1m, 0, TrendDirection.Stable)),
                new("svc-notification-hub", "Notification Hub", "Messaging", "Team Messaging",
                    CostEfficiency.Efficient, 1800m, TrendDirection.Improving,
                    Array.Empty<WasteSignalDto>(),
                    new ReliabilityCorrelationDto(99.2m, 0, TrendDirection.Improving)),
                new("svc-inventory-sync", "Inventory Sync", "Commerce", "Team Commerce",
                    CostEfficiency.Inefficient, 8900m, TrendDirection.Declining,
                    new[] { new WasteSignalDto("Redundant sync cycles", "redundant-sync", WasteSignalType.RepeatedReprocessing, 2800m) },
                    new ReliabilityCorrelationDto(81.0m, 2, TrendDirection.Declining)),
                new("svc-catalog-sync", "Catalog Sync", "Catalog", "Team Platform",
                    CostEfficiency.Wasteful, 15200m, TrendDirection.Declining,
                    new[] { new WasteSignalDto("Duplicate data processing pipelines", "duplicate-etl", WasteSignalType.RepeatedReprocessing, 4200m),
                            new WasteSignalDto("Idle staging environment", "idle-staging", WasteSignalType.IdleCostlyResource, 2500m) },
                    new ReliabilityCorrelationDto(65.4m, 4, TrendDirection.Declining))
            };

            var filtered = serviceIndicators.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(s => s.Team.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.DomainId))
                filtered = filtered.Where(s => s.Domain.Equals(request.DomainId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(s => s.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));

            var services = filtered.ToList();

            var topDrivers = services
                .OrderByDescending(s => s.MonthlyCost)
                .Take(3)
                .Select(s => new CostDriverDto(s.ServiceId, s.ServiceName, s.MonthlyCost, s.Efficiency))
                .ToList();

            var topWasteSignals = services
                .SelectMany(s => s.WasteSignals)
                .OrderByDescending(w => w.EstimatedWaste)
                .Take(5)
                .ToList();

            var optimizationOpportunities = services
                .Where(s => s.Efficiency is CostEfficiency.Wasteful or CostEfficiency.Inefficient)
                .Select(s => new OptimizationOpportunityDto(
                    s.ServiceId, s.ServiceName,
                    s.WasteSignals.Sum(w => w.EstimatedWaste),
                    s.Efficiency == CostEfficiency.Wasteful ? "High" : "Medium",
                    $"Address waste signals in {s.ServiceName}"))
                .ToList();

            var response = new Response(
                TotalMonthlyCost: services.Sum(s => s.MonthlyCost),
                TotalWaste: services.SelectMany(s => s.WasteSignals).Sum(w => w.EstimatedWaste),
                OverallEfficiency: CostEfficiency.Acceptable,
                CostTrend: TrendDirection.Stable,
                Services: services,
                TopCostDrivers: topDrivers,
                TopWasteSignals: topWasteSignals,
                OptimizationOpportunities: optimizationOpportunities,
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: true,
                DataSource: "demo");

            return Task.FromResult(Result<Response>.Success(response));
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
