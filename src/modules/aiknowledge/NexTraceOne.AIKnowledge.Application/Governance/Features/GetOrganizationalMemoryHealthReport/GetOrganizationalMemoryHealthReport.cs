using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetOrganizationalMemoryHealthReport;

/// <summary>
/// Wave BD.1 — GetOrganizationalMemoryHealthReport
/// Análise de saúde da memória organizacional: cobertura de tipos, frescor dos nós,
/// conectividade e relevância média. Permite ao Platform Admin e AI Lead avaliar
/// se o grafo de conhecimento organizacional está activo e bem estruturado.
/// </summary>
public static class GetOrganizationalMemoryHealthReport
{
    private static readonly string[] AllNodeTypes =
        ["decision", "incident", "contract_evolution", "pattern_learned", "adr"];

    public sealed record Query(
        Guid TenantId,
        int LookbackDays = 90,
        int StaleThresholdDays = 30) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.LookbackDays).InclusiveBetween(7, 365);
            RuleFor(x => x.StaleThresholdDays).InclusiveBetween(7, 180);
        }
    }

    public sealed class Handler(IOrganizationalMemoryRepository memoryRepo) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-request.LookbackDays);
            var staleCutoff = DateTimeOffset.UtcNow.AddDays(-request.StaleThresholdDays);

            // Collect all nodes per type
            var allTypeBreakdowns = new List<NodeTypeBreakdown>();
            int totalNodes = 0;
            int freshNodes = 0;
            int staleNodes = 0;
            int linkedNodes = 0;
            double totalRelevance = 0.0;
            var recentTitles = new List<string>();

            foreach (var nodeType in AllNodeTypes)
            {
                var nodes = await memoryRepo.ListByTypeAsync(nodeType, request.TenantId, ct);
                var inWindow = nodes.Where(n => n.RecordedAt >= cutoff).ToList();

                int typeFresh = inWindow.Count(n => n.RecordedAt >= staleCutoff);
                int typeStale = inWindow.Count(n => n.RecordedAt < staleCutoff);
                int typeLinked = inWindow.Count(n => n.LinkedNodeIds.Count > 0);
                double avgRel = inWindow.Count > 0
                    ? inWindow.Average(n => n.RelevanceScore)
                    : 0.0;

                allTypeBreakdowns.Add(new NodeTypeBreakdown(
                    nodeType,
                    inWindow.Count,
                    typeFresh,
                    typeStale,
                    typeLinked,
                    Math.Round(avgRel, 2)));

                totalNodes += inWindow.Count;
                freshNodes += typeFresh;
                staleNodes += typeStale;
                linkedNodes += typeLinked;
                totalRelevance += inWindow.Sum(n => n.RelevanceScore);

                recentTitles.AddRange(inWindow
                    .OrderByDescending(n => n.RecordedAt)
                    .Take(3)
                    .Select(n => n.Title));
            }

            double avgRelevance = totalNodes > 0 ? Math.Round(totalRelevance / totalNodes, 2) : 0.0;
            double freshnessRate = totalNodes > 0 ? Math.Round((double)freshNodes / totalNodes * 100, 1) : 0.0;
            double connectivityRate = totalNodes > 0 ? Math.Round((double)linkedNodes / totalNodes * 100, 1) : 0.0;

            var tier = ClassifyTier(totalNodes, freshnessRate, connectivityRate, avgRelevance);

            return new Response(
                TenantId: request.TenantId,
                TotalNodes: totalNodes,
                FreshNodes: freshNodes,
                StaleNodes: staleNodes,
                LinkedNodes: linkedNodes,
                FreshnessRatePct: freshnessRate,
                ConnectivityRatePct: connectivityRate,
                AverageRelevanceScore: avgRelevance,
                MemoryHealthTier: tier,
                NodeTypeBreakdown: allTypeBreakdowns.AsReadOnly(),
                RecentNodeTitles: recentTitles.Take(10).ToList().AsReadOnly(),
                LookbackDays: request.LookbackDays);
        }

        private static string ClassifyTier(int total, double freshRate, double connectRate, double avgRel)
        {
            if (total == 0) return "Empty";
            if (freshRate >= 70 && connectRate >= 40 && avgRel >= 0.7) return "Thriving";
            if (freshRate >= 50 && connectRate >= 20) return "Active";
            if (freshRate >= 30 || total >= 10) return "Building";
            return "Sparse";
        }
    }

    public sealed record NodeTypeBreakdown(
        string NodeType,
        int Count,
        int FreshCount,
        int StaleCount,
        int LinkedCount,
        double AverageRelevance);

    public sealed record Response(
        Guid TenantId,
        int TotalNodes,
        int FreshNodes,
        int StaleNodes,
        int LinkedNodes,
        double FreshnessRatePct,
        double ConnectivityRatePct,
        double AverageRelevanceScore,
        string MemoryHealthTier,
        IReadOnlyList<NodeTypeBreakdown> NodeTypeBreakdown,
        IReadOnlyList<string> RecentNodeTitles,
        int LookbackDays);
}
