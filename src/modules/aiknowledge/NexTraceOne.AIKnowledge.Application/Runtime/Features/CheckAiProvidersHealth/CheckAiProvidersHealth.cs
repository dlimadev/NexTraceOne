using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.CheckAiProvidersHealth;

/// <summary>
/// Feature: CheckAiProvidersHealth — verifica saúde de todos os providers de IA.
/// </summary>
public static class CheckAiProvidersHealth
{
    public sealed record Query() : IQuery<Response>;

    public sealed class Handler(
        IAiProviderHealthService healthService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var results = await healthService.CheckAllProvidersAsync(cancellationToken);

            var items = results.Select(r => new HealthItem(
                r.ProviderId,
                r.IsHealthy,
                r.Message,
                r.ResponseTime?.TotalMilliseconds)).ToList();

            var allHealthy = items.Count > 0 && items.All(i => i.IsHealthy);

            return new Response(allHealthy, items, items.Count);
        }
    }

    public sealed record Response(
        bool AllHealthy,
        IReadOnlyList<HealthItem> Items,
        int TotalCount);

    public sealed record HealthItem(
        string ProviderId,
        bool IsHealthy,
        string? Message,
        double? ResponseTimeMs);
}
