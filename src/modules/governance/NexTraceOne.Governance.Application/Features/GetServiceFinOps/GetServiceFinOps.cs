using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetServiceFinOps;

/// <summary>
/// Feature: GetServiceFinOps — perfil de custo contextual de um serviço individual.
/// Inclui waste, eficiência, correlação com confiabilidade e impacto de mudanças.
/// IMPLEMENTATION STATUS: Demo — returns illustrative data.
/// </summary>
public static class GetServiceFinOps
{
    /// <summary>Query para obter perfil de FinOps de um serviço.</summary>
    public sealed record Query(string ServiceId) : IQuery<Response>;

    /// <summary>Handler que retorna perfil de FinOps do serviço.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var wasteSignals = new List<WasteSignalDto>
            {
                new("Excessive retries on timeout", "retry-pattern", WasteSignalType.ExcessiveRetries, 3200m, "2026-03-10T08:00:00Z"),
                new("Over-provisioned compute instances", "over-provisioned", WasteSignalType.OverProvisioned, 2100m, "2026-03-12T14:00:00Z")
            };

            var efficiencyIndicators = new List<EfficiencyIndicatorDto>
            {
                new("CPU Utilization", EfficiencyCategory.ResourceUtilization, 42.5m, 75.0m, "Below optimal range"),
                new("Cost per Request", EfficiencyCategory.CostPerTransaction, 0.032m, 0.015m, "Above target threshold"),
                new("Error Rate Impact", EfficiencyCategory.ErrorRate, 4.2m, 1.0m, "Errors adding operational cost"),
                new("Throughput Efficiency", EfficiencyCategory.ThroughputOptimization, 68.0m, 85.0m, "Room for improvement")
            };

            var changeImpacts = new List<ChangeImpactDto>
            {
                new("chg-2026-0312", "Deploy v3.2.1 — Payment retry logic", "2026-03-12T10:00:00Z", 1200m, "Cost increase after deployment due to retry amplification"),
                new("chg-2026-0305", "Scale-up instance tier", "2026-03-05T16:00:00Z", 2800m, "Planned capacity increase for peak traffic")
            };

            var optimizations = new List<OptimizationDto>
            {
                new("Reduce retry backoff threshold", 1800m, "High", "Excessive retries are adding $1,800/mo in wasted compute"),
                new("Right-size compute instances", 2100m, "Medium", "CPU utilization consistently below 45% — downsize recommended"),
                new("Implement circuit breaker", 800m, "Medium", "Reduce cascading failure cost from upstream timeouts")
            };

            var response = new Response(
                ServiceId: request.ServiceId,
                ServiceName: "Payment API",
                Domain: "Payments",
                Team: "Team Payments",
                MonthlyCost: 12500m,
                PreviousMonthCost: 11200m,
                CostTrend: TrendDirection.Declining,
                Efficiency: CostEfficiency.Inefficient,
                WasteSignals: wasteSignals,
                TotalWaste: wasteSignals.Sum(w => w.EstimatedWaste),
                EfficiencyIndicators: efficiencyIndicators,
                ReliabilityScore: 72.5m,
                RecentIncidents: 3,
                ReliabilityTrend: TrendDirection.Declining,
                ChangeImpacts: changeImpacts,
                Optimizations: optimizations,
                TotalPotentialSavings: optimizations.Sum(o => o.PotentialSavings),
                GeneratedAt: DateTimeOffset.UtcNow,
                IsSimulated: true,
                DataSource: "demo");

            return Task.FromResult(Result<Response>.Success(response));
        }
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
