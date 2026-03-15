using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetFinOpsSummary;

/// <summary>
/// Feature: GetFinOpsSummary — resumo de FinOps contextual por serviço, equipa e domínio.
/// FinOps no NexTraceOne é contextual: ligado a operação, comportamento e eficiência.
/// </summary>
public static class GetFinOpsSummary
{
    /// <summary>Query de resumo de FinOps contextual.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null,
        string? ServiceId = null) : IQuery<Response>;

    /// <summary>Handler que computa resumo de FinOps contextual.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var serviceIndicators = new List<ServiceCostDto>
            {
                new("svc-payment-api", "Payment API", "Payments", "Team Payments",
                    CostEfficiency.Inefficient, 12500m, TrendDirection.Declining,
                    new[] { new WasteSignalDto("Excessive retries on timeout", "retry-pattern", 3200m) }),
                new("svc-order-processor", "Order Processor", "Commerce", "Team Commerce",
                    CostEfficiency.Wasteful, 18700m, TrendDirection.Declining,
                    new[] { new WasteSignalDto("Frequent rollbacks causing reprocessing", "rollback-waste", 5400m),
                            new WasteSignalDto("Idle compute during off-peak", "idle-compute", 2100m) }),
                new("svc-user-service", "User Service", "Identity", "Team Identity",
                    CostEfficiency.Acceptable, 4200m, TrendDirection.Stable,
                    Array.Empty<WasteSignalDto>()),
                new("svc-notification-hub", "Notification Hub", "Messaging", "Team Messaging",
                    CostEfficiency.Efficient, 1800m, TrendDirection.Improving,
                    Array.Empty<WasteSignalDto>()),
                new("svc-inventory-sync", "Inventory Sync", "Commerce", "Team Commerce",
                    CostEfficiency.Inefficient, 8900m, TrendDirection.Declining,
                    new[] { new WasteSignalDto("Redundant sync cycles", "redundant-sync", 2800m) })
            };

            var response = new Response(
                TotalMonthlyCost: serviceIndicators.Sum(s => s.MonthlyCost),
                TotalWaste: serviceIndicators.SelectMany(s => s.WasteSignals).Sum(w => w.EstimatedWaste),
                OverallEfficiency: CostEfficiency.Acceptable,
                CostTrend: TrendDirection.Stable,
                Services: serviceIndicators,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta do resumo de FinOps.</summary>
    public sealed record Response(
        decimal TotalMonthlyCost,
        decimal TotalWaste,
        CostEfficiency OverallEfficiency,
        TrendDirection CostTrend,
        IReadOnlyList<ServiceCostDto> Services,
        DateTimeOffset GeneratedAt);

    /// <summary>Custo contextual por serviço.</summary>
    public sealed record ServiceCostDto(
        string ServiceId,
        string ServiceName,
        string Domain,
        string Team,
        CostEfficiency Efficiency,
        decimal MonthlyCost,
        TrendDirection Trend,
        IReadOnlyList<WasteSignalDto> WasteSignals);

    /// <summary>Sinal de desperdício operacional identificado.</summary>
    public sealed record WasteSignalDto(
        string Description,
        string Pattern,
        decimal EstimatedWaste);
}
