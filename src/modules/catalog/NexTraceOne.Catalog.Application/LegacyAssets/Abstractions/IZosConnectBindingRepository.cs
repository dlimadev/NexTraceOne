using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;

/// <summary>
/// Repositório de bindings z/OS Connect do catálogo legacy.
/// </summary>
public interface IZosConnectBindingRepository
{
    Task<ZosConnectBinding?> GetByIdAsync(ZosConnectBindingId id, CancellationToken cancellationToken);
    Task<ZosConnectBinding?> GetByNameAndSystemAsync(string name, MainframeSystemId systemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ZosConnectBinding>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken);
    void Add(ZosConnectBinding binding);
}
