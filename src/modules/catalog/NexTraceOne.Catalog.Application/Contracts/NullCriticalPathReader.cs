using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>Wave AR.2 — honest-null ICriticalPathReader.</summary>
public sealed class NullCriticalPathReader : ICriticalPathReader
{
    public Task<IReadOnlyList<IServiceTopologyReader.ServiceDependencyEntry>> ListDependenciesByTenantAsync(
        string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IServiceTopologyReader.ServiceDependencyEntry>>([]);

    public Task<IReadOnlyList<IServiceTopologyReader.ServiceNodeEntry>> ListServiceNodesByTenantAsync(
        string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IServiceTopologyReader.ServiceNodeEntry>>([]);
}
