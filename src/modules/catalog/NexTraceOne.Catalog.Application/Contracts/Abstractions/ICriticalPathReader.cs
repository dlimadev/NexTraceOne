namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>Wave AR.2 — GetCriticalPathReport.</summary>
public interface ICriticalPathReader
{
    Task<IReadOnlyList<IServiceTopologyReader.ServiceDependencyEntry>> ListDependenciesByTenantAsync(
        string tenantId, CancellationToken ct);

    Task<IReadOnlyList<IServiceTopologyReader.ServiceNodeEntry>> ListServiceNodesByTenantAsync(
        string tenantId, CancellationToken ct);
}
