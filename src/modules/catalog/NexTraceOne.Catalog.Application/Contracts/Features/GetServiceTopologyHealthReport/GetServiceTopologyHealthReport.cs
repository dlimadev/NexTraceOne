using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetServiceTopologyHealthReport;

/// <summary>
/// Feature: GetServiceTopologyHealthReport — saúde do grafo de dependências.
/// Wave AR.1 — Service Topology Intelligence &amp; Dependency Mapping.
/// </summary>
public static class GetServiceTopologyHealthReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int FreshnessDays = 30,
        int HubFanInThreshold = 5,
        int HubPenalty = 15,
        int CircularPenalty = 20) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(x => x.FreshnessDays).InclusiveBetween(1, 365);
            RuleFor(x => x.HubFanInThreshold).GreaterThan(0);
            RuleFor(x => x.HubPenalty).InclusiveBetween(0, 100);
            RuleFor(x => x.CircularPenalty).InclusiveBetween(0, 100);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum TopologyHealthTier { Healthy, Warning, Degraded, Critical }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        int TotalServices,
        int TotalDependencies,
        double AvgFanOut,
        double AvgFanIn,
        double GraphDensity,
        double TopologyFreshnessScore,
        double TenantTopologyHealthScore,
        TopologyHealthTier Tier,
        IReadOnlyList<string> OrphanServices,
        IReadOnlyList<string> HubServices,
        IReadOnlyList<IReadOnlyList<string>> CircularDependencies,
        int IsolatedClusterCount,
        IReadOnlyList<string> StaleTopologyServices,
        IReadOnlyList<string> ArchitectureRecommendations);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IServiceTopologyReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var staleThreshold = now.AddDays(-request.FreshnessDays);

            var deps = await reader.ListDependenciesByTenantAsync(
                request.TenantId, request.FreshnessDays, cancellationToken);
            var nodes = await reader.ListServiceNodesByTenantAsync(
                request.TenantId, cancellationToken);

            var allServiceIds = nodes.Select(n => n.ServiceId).ToHashSet();
            foreach (var d in deps)
            {
                allServiceIds.Add(d.SourceServiceId);
                allServiceIds.Add(d.TargetServiceId);
            }

            int totalServices = allServiceIds.Count;
            int totalDeps = deps.Count;

            var adjacency = new Dictionary<string, List<string>>();
            var fanIn = new Dictionary<string, int>();
            var fanOut = new Dictionary<string, int>();

            foreach (var svc in allServiceIds)
            {
                adjacency[svc] = [];
                fanIn[svc] = 0;
                fanOut[svc] = 0;
            }

            foreach (var d in deps)
            {
                adjacency[d.SourceServiceId].Add(d.TargetServiceId);
                fanOut[d.SourceServiceId] = fanOut[d.SourceServiceId] + 1;
                fanIn[d.TargetServiceId] = fanIn[d.TargetServiceId] + 1;
            }

            double avgFanOut = totalServices == 0 ? 0.0 : Math.Round((double)totalDeps / totalServices, 2);
            double avgFanIn = avgFanOut;

            double graphDensity = totalServices <= 1 ? 0.0
                : Math.Min(1.0, (double)totalDeps / (totalServices * (totalServices - 1)));
            graphDensity = Math.Round(graphDensity, 4);

            var orphanServices = allServiceIds
                .Where(s => fanIn[s] == 0 && fanOut[s] == 0)
                .OrderBy(s => s)
                .ToList();

            var hubServices = allServiceIds
                .Where(s => fanIn[s] >= request.HubFanInThreshold)
                .OrderByDescending(s => fanIn[s])
                .ToList();

            var staleDepsServiceIds = deps
                .Where(d => d.LastUpdatedAt < staleThreshold)
                .SelectMany(d => new[] { d.SourceServiceId, d.TargetServiceId })
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            int staleDepsCount = deps.Count(d => d.LastUpdatedAt < staleThreshold);
            double freshnessScore = totalDeps == 0 ? 100.0
                : Math.Round((double)(totalDeps - staleDepsCount) / totalDeps * 100.0, 2);

            var cycles = new List<IReadOnlyList<string>>();
            var visited = new HashSet<string>();
            var recStack = new HashSet<string>();
            var path = new List<string>();

            foreach (var node in adjacency.Keys)
            {
                if (!visited.Contains(node))
                    DetectCycle(node, adjacency, visited, recStack, path, cycles, 10);
            }

            var undirected = new Dictionary<string, HashSet<string>>();
            foreach (var svc in allServiceIds)
                undirected[svc] = [];

            foreach (var d in deps)
            {
                undirected[d.SourceServiceId].Add(d.TargetServiceId);
                undirected[d.TargetServiceId].Add(d.SourceServiceId);
            }

            var componentMap = new Dictionary<string, int>();
            int compId = 0;
            foreach (var node in allServiceIds)
            {
                if (componentMap.ContainsKey(node)) continue;
                var queue = new Queue<string>();
                queue.Enqueue(node);
                componentMap[node] = compId;
                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    foreach (var neighbour in undirected.GetValueOrDefault(cur, []))
                    {
                        if (!componentMap.ContainsKey(neighbour))
                        {
                            componentMap[neighbour] = compId;
                            queue.Enqueue(neighbour);
                        }
                    }
                }
                compId++;
            }

            int isolatedClusterCount = 0;
            if (componentMap.Count > 0)
            {
                var componentSizes = componentMap.Values.GroupBy(x => x).Select(g => g.Count()).ToList();
                int largestSize = componentSizes.Count > 0 ? componentSizes.Max() : 0;
                // Components smaller than the largest are considered isolated from the main cluster.
                // When all components are equal size (e.g. two disconnected pairs), all but one are isolated.
                int componentsOfLargestSize = componentSizes.Count(s => s == largestSize);
                isolatedClusterCount = componentSizes.Count - componentsOfLargestSize
                    + Math.Max(0, componentsOfLargestSize - 1);
            }

            int circularCount = cycles.Count;
            int hubCount = hubServices.Count;

            double baseScore = 100.0
                - (circularCount * request.CircularPenalty)
                - (hubCount * request.HubPenalty);

            double freshnessContrib = freshnessScore * 0.40;
            double densityScore = graphDensity > 0.5 ? (1 - graphDensity) * 100 : 100.0;
            double densityContrib = densityScore * 0.30;
            double healthScore = Math.Clamp(baseScore * 0.30 + freshnessContrib + densityContrib, 0, 100);
            healthScore = Math.Round(healthScore, 2);

            TopologyHealthTier tier;
            if (circularCount > 2 || healthScore < 30)
                tier = TopologyHealthTier.Critical;
            else if (circularCount > 0 || healthScore < 50 || hubCount > 5)
                tier = TopologyHealthTier.Degraded;
            else if (circularCount == 0 && hubCount <= 2 && freshnessScore >= 90)
                tier = TopologyHealthTier.Healthy;
            else
                tier = TopologyHealthTier.Warning;

            var recommendations = new List<string>();
            if (cycles.Count > 0 && recommendations.Count < 3)
                recommendations.Add($"Resolve circular dependency: {string.Join(" → ", cycles[0])}");
            if (hubServices.Count > 0 && recommendations.Count < 3)
                recommendations.Add($"Reduce fan-in for hub service: {hubServices[0]}");
            if (staleDepsServiceIds.Count > 0 && recommendations.Count < 3)
                recommendations.Add($"Update stale topology for: {staleDepsServiceIds[0]}");

            var report = new Report(
                now, request.TenantId, request.LookbackDays,
                totalServices, totalDeps,
                avgFanOut, avgFanIn,
                graphDensity, freshnessScore, healthScore, tier,
                orphanServices, hubServices, cycles,
                isolatedClusterCount, staleDepsServiceIds, recommendations);

            return Result<Report>.Success(report);
        }

        private static void DetectCycle(
            string node,
            Dictionary<string, List<string>> adj,
            HashSet<string> visited,
            HashSet<string> recStack,
            List<string> path,
            List<IReadOnlyList<string>> cycles,
            int maxCycles)
        {
            if (cycles.Count >= maxCycles) return;

            visited.Add(node);
            recStack.Add(node);
            path.Add(node);

            foreach (var neighbour in adj.GetValueOrDefault(node, []))
            {
                if (cycles.Count >= maxCycles) break;

                if (recStack.Contains(neighbour))
                {
                    int startIdx = path.IndexOf(neighbour);
                    if (startIdx >= 0)
                        cycles.Add(path[startIdx..].ToList());
                }
                else if (!visited.Contains(neighbour))
                {
                    DetectCycle(neighbour, adj, visited, recStack, path, cycles, maxCycles);
                }
            }

            recStack.Remove(node);
            if (path.Count > 0) path.RemoveAt(path.Count - 1);
        }
    }
}
