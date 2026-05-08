using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAgentPerformanceBenchmarkReport;

/// <summary>
/// Wave BD.2 — GetAgentPerformanceBenchmarkReport
/// Relatório de benchmark cruzado entre agents: compõe um BenchmarkScore ponderado
/// (accuracy 40 %, rating 30 %, feedback coverage 20 %, RL bonus 10 %) e classifica
/// cada agent num tier: Champion / HighPerformer / Active / Developing / Underperforming.
/// Permite ao AI Lead identificar agents de destaque e agents que precisam de atenção.
/// </summary>
public static class GetAgentPerformanceBenchmarkReport
{
    public sealed record Query(
        Guid TenantId,
        int MinExecutions = 5) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.MinExecutions).InclusiveBetween(1, 1000);
        }
    }

    public sealed class Handler(
        IAiAgentPerformanceMetricRepository metricRepo) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var allMetrics = await metricRepo.ListByTenantAsync(request.TenantId, ct);

            var qualified = allMetrics
                .Where(m => m.TotalExecutions >= request.MinExecutions)
                .ToList();

            if (qualified.Count == 0)
                return new Response(
                    TenantId: request.TenantId,
                    TotalAgentsEvaluated: allMetrics.Count,
                    QualifiedAgents: 0,
                    AgentBenchmarks: [],
                    TierSummary: BuildEmptyTierSummary(),
                    TopPerformerName: null,
                    AverageBenchmarkScore: 0.0);

            var benchmarks = qualified.Select(m =>
            {
                double feedbackCoverage = m.TotalExecutions > 0
                    ? (double)m.ExecutionsWithFeedback / m.TotalExecutions
                    : 0.0;

                double normalizedRating = m.AverageRating / 5.0;

                double rlBonus = Math.Min(m.RlCyclesCompleted / 10.0, 1.0);

                double score = Math.Round(
                    m.AccuracyRate * 0.40 +
                    normalizedRating * 0.30 +
                    feedbackCoverage * 0.20 +
                    rlBonus * 0.10, 3);

                string tier = ClassifyTier(score);

                return new AgentBenchmarkItem(
                    AgentId: m.AgentId.Value,
                    AgentName: m.AgentName,
                    TotalExecutions: m.TotalExecutions,
                    AccuracyRate: Math.Round(m.AccuracyRate, 3),
                    AverageRating: Math.Round(m.AverageRating, 2),
                    FeedbackCoveragePct: Math.Round(feedbackCoverage * 100, 1),
                    RlCyclesCompleted: m.RlCyclesCompleted,
                    BenchmarkScore: score,
                    BenchmarkTier: tier);
            })
            .OrderByDescending(b => b.BenchmarkScore)
            .ToList()
            .AsReadOnly();

            double avgScore = Math.Round(benchmarks.Average(b => b.BenchmarkScore), 3);

            var tierSummary = BuildTierSummary(benchmarks);

            return new Response(
                TenantId: request.TenantId,
                TotalAgentsEvaluated: allMetrics.Count,
                QualifiedAgents: qualified.Count,
                AgentBenchmarks: benchmarks,
                TierSummary: tierSummary,
                TopPerformerName: benchmarks[0].AgentName,
                AverageBenchmarkScore: avgScore);
        }

        private static string ClassifyTier(double score) => score switch
        {
            >= 0.80 => "Champion",
            >= 0.65 => "HighPerformer",
            >= 0.50 => "Active",
            >= 0.30 => "Developing",
            _ => "Underperforming"
        };

        private static IReadOnlyDictionary<string, int> BuildTierSummary(
            IReadOnlyList<AgentBenchmarkItem> items)
        {
            var tiers = new[] { "Champion", "HighPerformer", "Active", "Developing", "Underperforming" };
            return tiers.ToDictionary(t => t, t => items.Count(i => i.BenchmarkTier == t));
        }

        private static IReadOnlyDictionary<string, int> BuildEmptyTierSummary()
        {
            var tiers = new[] { "Champion", "HighPerformer", "Active", "Developing", "Underperforming" };
            return tiers.ToDictionary(t => t, _ => 0);
        }
    }

    public sealed record AgentBenchmarkItem(
        Guid AgentId,
        string AgentName,
        long TotalExecutions,
        double AccuracyRate,
        double AverageRating,
        double FeedbackCoveragePct,
        int RlCyclesCompleted,
        double BenchmarkScore,
        string BenchmarkTier);

    public sealed record Response(
        Guid TenantId,
        int TotalAgentsEvaluated,
        int QualifiedAgents,
        IReadOnlyList<AgentBenchmarkItem> AgentBenchmarks,
        IReadOnlyDictionary<string, int> TierSummary,
        string? TopPerformerName,
        double AverageBenchmarkScore);
}
