using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>Wave AR.1 — honest-null IServiceTopologyReader.</summary>
public sealed class NullServiceTopologyReader : IServiceTopologyReader
{
    public Task<IReadOnlyList<IServiceTopologyReader.ServiceDependencyEntry>> ListDependenciesByTenantAsync(
        string tenantId, int freshnessThresholdDays, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IServiceTopologyReader.ServiceDependencyEntry>>([]);

    public Task<IReadOnlyList<IServiceTopologyReader.ServiceNodeEntry>> ListServiceNodesByTenantAsync(
        string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IServiceTopologyReader.ServiceNodeEntry>>([]);
}
