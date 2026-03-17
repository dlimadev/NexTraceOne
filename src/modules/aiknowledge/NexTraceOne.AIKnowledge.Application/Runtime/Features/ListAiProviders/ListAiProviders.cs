using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.ListAiProviders;

/// <summary>
/// Feature: ListAiProviders — lista todos os providers de IA registrados com status.
/// </summary>
public static class ListAiProviders
{
    public sealed record Query() : IQuery<Response>;

    public sealed class Handler(
        IAiProviderFactory providerFactory,
        IAiProviderHealthService healthService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var providers = providerFactory.GetAllProviders();
            var healthResults = await healthService.CheckAllProvidersAsync(cancellationToken);

            var items = providers.Select(p =>
            {
                var health = healthResults.FirstOrDefault(h => h.ProviderId == p.ProviderId);
                return new ProviderItem(
                    p.ProviderId,
                    p.DisplayName,
                    p.IsLocal,
                    health?.IsHealthy ?? false,
                    health?.Message,
                    health?.ResponseTime?.TotalMilliseconds);
            }).ToList();

            return new Response(items, items.Count);
        }
    }

    public sealed record Response(
        IReadOnlyList<ProviderItem> Items,
        int TotalCount);

    public sealed record ProviderItem(
        string ProviderId,
        string DisplayName,
        bool IsLocal,
        bool IsHealthy,
        string? HealthMessage,
        double? ResponseTimeMs);
}
