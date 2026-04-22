using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetCriticalPathReport;

/// <summary>
/// Feature: GetCriticalPathReport — análise de caminho crítico de dependências.
/// Wave AR.2 — Service Topology Intelligence &amp; Dependency Mapping.
/// </summary>
public static class GetCriticalPathReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int TopNChains = 10,
        int BottleneckPathCount = 3) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(x => x.TopNChains).InclusiveBetween(1, 100);
            RuleFor(x => x.BottleneckPathCount).GreaterThan(0);
        }
    }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record CriticalPathChain(
        IReadOnlyList<string> Path,
        int Depth,
        bool CustomerFacingAtRoot,
        int TotalServiceTierRisk);

    public sealed record CascadeRiskEntry(
        string ServiceId,
        double CascadeRiskScore,
        int FanOut,
        int ChainsPresent,
        bool HasCustomerFacingDownstream);

    public sealed record DepthDistributionEntry(
        int MinDepth,
        int ServiceCount);

    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        int TotalServices,
        int TotalDependencies,
        int MaxDependencyDepth,
        double TenantCriticalPathIndex,
        IReadOnlyList<CriticalPathChain> CriticalPathChains,
        IReadOnlyList<string> BottleneckServices,
        IReadOnlyList<CascadeRiskEntry> TopCascadeRiskServices,
        IReadOnlyList<DepthDistributionEntry> DepthDistribution);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        ICriticalPathReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;

            var deps = await reader.ListDependenciesByTenantAsync(request.TenantId, cancellationToken);
            var nodes = await reader.ListServiceNodesByTenantAsync(request.TenantId, cancellationToken);

            var allServiceIds = nodes.Select(n => n.ServiceId).ToList();
            foreach (var d in deps)
            {
                if (!allServiceIds.Contains(d.SourceServiceId)) allServiceIds.Add(d.SourceServiceId);
                if (!allServiceIds.Contains(d.TargetServiceId)) allServiceIds.Add(d.TargetServiceId);
            }

            int totalServices = allServiceIds.Count;
            int totalDeps = deps.Count;

            if (totalDeps == 0)
            {
                var emptyReport = new Report(
                    now, request.TenantId, request.LookbackDays,
                    totalServices, 0, 0, 0.0,
                    [], [], [], []);
                return Result<Report>.Success(emptyReport);
            }

            // Build directed adjacency
            var adj = new Dictionary<string, List<string>>();
            var fanOut = new Dictionary<string, int>();
            var customerFacing = nodes.ToDictionary(n => n.ServiceId, n => n.IsCustomerFacing);
            var tierMap = new Dictionary<string, string>();
            foreach (var n in nodes) tierMap[n.ServiceId] = n.ServiceTier;
            foreach (var d in deps)
            {
                tierMap.TryAdd(d.SourceServiceId, d.SourceServiceTier);
                tierMap.TryAdd(d.TargetServiceId, d.TargetServiceTier);
            }

            foreach (var svc in allServiceIds)
            {
                adj[svc] = [];
                fanOut[svc] = 0;
            }

            foreach (var d in deps)
            {
                adj[d.SourceServiceId].Add(d.TargetServiceId);
                fanOut[d.SourceServiceId] = fanOut[d.SourceServiceId] + 1;
            }

            // Find longest chains
            var allChains = FindLongestChains(adj, allServiceIds, request.TopNChains);

            int maxDepth = allChains.Count > 0 ? allChains.Max(c => c.Count) : 0;

            // Build CriticalPathChain records
            var chainRecords = allChains.Select(chain =>
            {
                string root = chain[0];
                bool cfAtRoot = customerFacing.GetValueOrDefault(root, false);
                int tierRisk = chain.Sum(s => TierToRisk(tierMap.GetValueOrDefault(s, "Internal")));
                return new CriticalPathChain(chain, chain.Count, cfAtRoot, tierRisk);
            }).ToList();

            // Bottleneck services: appear in >= BottleneckPathCount chains
            var chainPresenceCount = new Dictionary<string, int>();
            foreach (var chain in allChains)
            {
                foreach (var svc in chain)
                    chainPresenceCount[svc] = chainPresenceCount.GetValueOrDefault(svc) + 1;
            }

            var bottleneckServices = chainPresenceCount
                .Where(kv => kv.Value >= request.BottleneckPathCount)
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            // Cascade risk scores
            int maxFanOut = fanOut.Count > 0 ? fanOut.Values.Max() : 1;
            int totalChains = allChains.Count;

            var cascadeRiskEntries = allServiceIds.Select(svc =>
            {
                int fo = fanOut.GetValueOrDefault(svc);
                int chainCount = chainPresenceCount.GetValueOrDefault(svc);

                // Check if any downstream node is customer-facing
                bool hasCfDownstream = HasCustomerFacingDownstream(svc, adj, customerFacing, new HashSet<string>(), 0, 10);

                double fanOutScore = maxFanOut == 0 ? 0 : (double)fo / maxFanOut * 100 * 0.40;
                double pathScore = totalChains == 0 ? 0 : (double)chainCount / totalChains * 100 * 0.40;
                double cfScore = hasCfDownstream ? 20.0 : 0.0;
                double cascadeScore = Math.Clamp(Math.Round(fanOutScore + pathScore + cfScore, 2), 0, 100);

                return new CascadeRiskEntry(svc, cascadeScore, fo, chainCount, hasCfDownstream);
            })
            .OrderByDescending(e => e.CascadeRiskScore)
            .Take(10)
            .ToList();

            // Depth distribution
            var maxDepthPerService = new Dictionary<string, int>();
            foreach (var chain in allChains)
            {
                for (int i = 0; i < chain.Count; i++)
                {
                    // Depth at position = chain length (depth from root to leaf = chain count)
                    int depthOfChain = chain.Count;
                    string svc = chain[i];
                    if (!maxDepthPerService.TryGetValue(svc, out int existing) || existing < depthOfChain)
                        maxDepthPerService[svc] = depthOfChain;
                }
            }

            var depthDist = new List<DepthDistributionEntry>
            {
                new(3, maxDepthPerService.Values.Count(d => d >= 3)),
                new(5, maxDepthPerService.Values.Count(d => d >= 5)),
                new(8, maxDepthPerService.Values.Count(d => d >= 8))
            };

            // TenantCriticalPathIndex
            bool hasCycles = chainPresenceCount.Count > 0 && allChains.Any(c => c.Count == 0); // simplified
            double criticalPathIndex = Math.Clamp(
                maxDepth / 10.0 * 30
                + (cascadeRiskEntries.Count > 5 ? 30 : 15),
                0, 100);
            criticalPathIndex = Math.Round(criticalPathIndex, 2);

            var report = new Report(
                now, request.TenantId, request.LookbackDays,
                totalServices, totalDeps,
                maxDepth, criticalPathIndex,
                chainRecords, bottleneckServices, cascadeRiskEntries, depthDist);

            return Result<Report>.Success(report);
        }

        private static int TierToRisk(string tier) => tier switch
        {
            "Critical" => 3,
            "Standard" => 2,
            _ => 1
        };

        private static bool HasCustomerFacingDownstream(
            string node,
            Dictionary<string, List<string>> adj,
            Dictionary<string, bool> customerFacing,
            HashSet<string> visited,
            int depth,
            int maxDepth)
        {
            if (depth > maxDepth || visited.Contains(node)) return false;
            visited.Add(node);
            foreach (var neighbour in adj.GetValueOrDefault(node, []))
            {
                if (customerFacing.GetValueOrDefault(neighbour, false)) return true;
                if (HasCustomerFacingDownstream(neighbour, adj, customerFacing, visited, depth + 1, maxDepth))
                    return true;
            }
            return false;
        }

        private static IReadOnlyList<IReadOnlyList<string>> FindLongestChains(
            Dictionary<string, List<string>> adj,
            IReadOnlyList<string> allNodes,
            int topN)
        {
            var allChains = new List<List<string>>();

            var hasIncoming = new HashSet<string>(adj.Values.SelectMany(v => v));
            var roots = allNodes.Where(n => !hasIncoming.Contains(n)).ToList();

            if (roots.Count == 0) roots = allNodes.ToList();

            foreach (var root in roots)
            {
                var chains = DfsChains(root, adj, new HashSet<string>(), []);
                allChains.AddRange(chains);
            }

            return allChains
                .OrderByDescending(c => c.Count)
                .Take(topN)
                .Select(c => (IReadOnlyList<string>)c)
                .ToList();
        }

        private static IReadOnlyList<List<string>> DfsChains(
            string node,
            Dictionary<string, List<string>> adj,
            HashSet<string> visitedInPath,
            List<string> currentPath)
        {
            var newPath = currentPath.Append(node).ToList();

            if (visitedInPath.Contains(node)
                || !adj.TryGetValue(node, out var neighbors)
                || !neighbors.Any())
                return [newPath];

            var newVisited = new HashSet<string>(visitedInPath) { node };
            var result = new List<List<string>>();

            foreach (var neighbour in neighbors)
            {
                if (!newVisited.Contains(neighbour))
                    result.AddRange(DfsChains(neighbour, adj, newVisited, newPath));
            }

            return result.Count > 0 ? result : [newPath];
        }
    }
}
