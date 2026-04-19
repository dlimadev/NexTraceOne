using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgentPerformanceDashboard;

/// <summary>
/// Feature: GetAgentPerformanceDashboard — retorna métricas agregadas de performance para Agent Lightning.
/// Apresenta accuracy rate, tendência, ciclos RL e trajectórias exportadas por agent.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetAgentPerformanceDashboard
{
    /// <summary>Query do dashboard de performance de agents.</summary>
    public sealed record Query(Guid TenantId) : IQuery<Response>;

    /// <summary>Handler que carrega e agrega as métricas de performance por tenant.</summary>
    public sealed class Handler(
        IAiAgentPerformanceMetricRepository metricRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var metrics = await metricRepository.ListByTenantAsync(request.TenantId, cancellationToken);

            if (metrics.Count == 0)
                return new Response([], 0, 0, 0);

            var items = metrics.Select(m => new PerformanceItem(
                AgentId: m.AgentId.Value,
                AgentName: m.AgentName,
                AccuracyRate30d: m.AccuracyRate,
                AccuracyTrend: ComputeTrend(m.AccuracyRate),
                RlCyclesCompleted: m.RlCyclesCompleted,
                TrajectoriesExported: m.TrajectoriesExported)).ToList().AsReadOnly();

            var totalTrajectoriesCollected = metrics.Sum(m => m.TrajectoriesExported);
            var withFeedbackConfirmed = metrics.Sum(m => m.ExecutionsWithFeedback);
            var totalRlCycles = metrics.Sum(m => m.RlCyclesCompleted);

            return new Response(
                AgentItems: items,
                TotalTrajectoriesCollected: totalTrajectoriesCollected,
                WithFeedbackConfirmed: withFeedbackConfirmed,
                TotalRlCyclesCompleted: totalRlCycles);
        }

        private static string ComputeTrend(double accuracyRate) => accuracyRate switch
        {
            >= 0.8 => "improving",
            >= 0.5 => "stable",
            _ => "degrading"
        };
    }

    /// <summary>Item de performance por agent.</summary>
    public sealed record PerformanceItem(
        Guid AgentId,
        string AgentName,
        double AccuracyRate30d,
        string AccuracyTrend,
        int RlCyclesCompleted,
        long TrajectoriesExported);

    /// <summary>Resposta do dashboard de performance.</summary>
    public sealed record Response(
        IReadOnlyList<PerformanceItem> AgentItems,
        long TotalTrajectoriesCollected,
        long WithFeedbackConfirmed,
        int TotalRlCyclesCompleted);
}
