namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Port for observability backend health checks.
/// Implemented in Infrastructure by delegating to IObservabilityProvider.
/// Keeps Application layer free of BuildingBlocks.Observability dependency.
/// </summary>
public interface IObservabilityBackendHealth
{
    /// <summary>Active provider name: "Elastic" or "ClickHouse".</summary>
    string ProviderName { get; }

    /// <summary>Returns true when the backend responds to health checks.</summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
