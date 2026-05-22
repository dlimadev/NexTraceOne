using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Infrastructure.Readers;

/// <summary>
/// Implementação real de ICriticalPathReader.
/// Delega para IServiceTopologyReader com threshold de frescura fixo de 90 dias,
/// focando relações de dependência estabelecidas e estáveis.
/// Wave AR.2 — GetCriticalPathReport.
/// </summary>
internal sealed class EfCriticalPathReader(IServiceTopologyReader topologyReader) : ICriticalPathReader
{
    private const int FreshnessDays = 90;

    public Task<IReadOnlyList<IServiceTopologyReader.ServiceDependencyEntry>> ListDependenciesByTenantAsync(
        string tenantId, CancellationToken ct)
        => topologyReader.ListDependenciesByTenantAsync(tenantId, FreshnessDays, ct);

    public Task<IReadOnlyList<IServiceTopologyReader.ServiceNodeEntry>> ListServiceNodesByTenantAsync(
        string tenantId, CancellationToken ct)
        => topologyReader.ListServiceNodesByTenantAsync(tenantId, ct);
}
