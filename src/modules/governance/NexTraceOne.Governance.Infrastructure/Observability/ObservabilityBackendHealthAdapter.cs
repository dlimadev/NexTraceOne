using NexTraceOne.BuildingBlocks.Observability.Observability.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Infrastructure.Observability;

/// <summary>
/// Adapts IObservabilityProvider (BuildingBlocks) to IObservabilityBackendHealth (Application port).
/// </summary>
internal sealed class ObservabilityBackendHealthAdapter(IObservabilityProvider provider) : IObservabilityBackendHealth
{
    public string ProviderName => provider.ProviderName;

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => provider.IsHealthyAsync(cancellationToken);
}
