using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetObservabilityProvider;

/// <summary>
/// Feature: GetObservabilityProvider — returns the active non-relational backend and its health.
/// Useful for operators diagnosing why SimulatedNote appears or confirming provider switch.
/// </summary>
public static class GetObservabilityProvider
{
    /// <summary>Query — no parameters required.</summary>
    public sealed record Query : IQuery<Response>;

    /// <summary>Handler that reports the active observability backend and health status.</summary>
    public sealed class Handler(IObservabilityBackendHealth health) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var healthy = await health.IsHealthyAsync(cancellationToken);

            var response = new Response(
                Provider: health.ProviderName,
                Healthy: healthy,
                CheckedAt: DateTimeOffset.UtcNow);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Active observability provider status.</summary>
    public sealed record Response(
        string Provider,
        bool Healthy,
        DateTimeOffset CheckedAt);
}
