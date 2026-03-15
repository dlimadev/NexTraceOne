using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetEfficiencyIndicators;

/// <summary>
/// Feature: GetEfficiencyIndicators — indicadores de eficiência operacional filtrados por serviço ou equipa.
/// Eficiência no NexTraceOne mede a relação entre custo e valor operacional real.
/// </summary>
public static class GetEfficiencyIndicators
{
    /// <summary>Query para obter indicadores de eficiência.</summary>
    public sealed record Query(
        string? ServiceId = null,
        string? TeamId = null) : IQuery<Response>;

    /// <summary>Handler que retorna indicadores de eficiência operacional.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var indicators = new List<ServiceEfficiencyDto>
            {
                new("svc-payment-api", "Payment API", "Team Payments", CostEfficiency.Inefficient, new EfficiencyMetricDto[]
                {
                    new("CPU Utilization", EfficiencyCategory.ResourceUtilization, 42.5m, 75.0m, "%", "Below optimal range"),
                    new("Cost per Request", EfficiencyCategory.CostPerTransaction, 0.032m, 0.015m, "USD", "Above target"),
                    new("Error Rate", EfficiencyCategory.ErrorRate, 4.2m, 1.0m, "%", "Errors contributing to waste"),
                    new("Throughput Efficiency", EfficiencyCategory.ThroughputOptimization, 68.0m, 85.0m, "%", "Below target")
                }),
                new("svc-order-processor", "Order Processor", "Team Commerce", CostEfficiency.Wasteful, new EfficiencyMetricDto[]
                {
                    new("CPU Utilization", EfficiencyCategory.ResourceUtilization, 35.0m, 75.0m, "%", "Significantly under-utilized"),
                    new("Cost per Request", EfficiencyCategory.CostPerTransaction, 0.048m, 0.015m, "USD", "Very high cost per request"),
                    new("Error Rate", EfficiencyCategory.ErrorRate, 8.1m, 1.0m, "%", "High error rate amplifying cost"),
                    new("Scaling Efficiency", EfficiencyCategory.ScalingEfficiency, 52.0m, 80.0m, "%", "Poor auto-scaling response")
                }),
                new("svc-notification-hub", "Notification Hub", "Team Messaging", CostEfficiency.Efficient, new EfficiencyMetricDto[]
                {
                    new("CPU Utilization", EfficiencyCategory.ResourceUtilization, 72.0m, 75.0m, "%", "Near optimal"),
                    new("Cost per Request", EfficiencyCategory.CostPerTransaction, 0.008m, 0.015m, "USD", "Below target — efficient"),
                    new("Error Rate", EfficiencyCategory.ErrorRate, 0.3m, 1.0m, "%", "Very low error rate"),
                    new("Throughput Efficiency", EfficiencyCategory.ThroughputOptimization, 91.0m, 85.0m, "%", "Above target")
                })
            };

            var filtered = indicators.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.ServiceId))
                filtered = filtered.Where(s => s.ServiceId.Equals(request.ServiceId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(request.TeamId))
                filtered = filtered.Where(s => s.Team.Equals(request.TeamId, StringComparison.OrdinalIgnoreCase));

            var result = filtered.ToList();

            // Normaliza pontuação considerando que para algumas categorias menor é melhor.
            static decimal NormalizeMetric(EfficiencyMetricDto m)
            {
                if (m.TargetValue == 0) return 100m;

                var isLowerBetter = m.Category is EfficiencyCategory.ErrorRate
                    or EfficiencyCategory.CostPerTransaction;

                return isLowerBetter
                    ? Math.Min(m.TargetValue / m.CurrentValue * 100, 200m)
                    : Math.Min(m.CurrentValue / m.TargetValue * 100, 200m);
            }

            var overallScore = result.Count > 0
                ? result.Average(s => s.Metrics.Average(NormalizeMetric))
                : 0m;

            var response = new Response(
                OverallEfficiencyScore: Math.Round(overallScore, 1),
                ServiceCount: result.Count,
                Services: result,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com indicadores de eficiência.</summary>
    public sealed record Response(
        decimal OverallEfficiencyScore,
        int ServiceCount,
        IReadOnlyList<ServiceEfficiencyDto> Services,
        DateTimeOffset GeneratedAt);

    /// <summary>Eficiência operacional de um serviço.</summary>
    public sealed record ServiceEfficiencyDto(
        string ServiceId,
        string ServiceName,
        string Team,
        CostEfficiency Efficiency,
        IReadOnlyList<EfficiencyMetricDto> Metrics);

    /// <summary>Métrica individual de eficiência.</summary>
    public sealed record EfficiencyMetricDto(
        string Name,
        EfficiencyCategory Category,
        decimal CurrentValue,
        decimal TargetValue,
        string Unit,
        string Assessment);
}
