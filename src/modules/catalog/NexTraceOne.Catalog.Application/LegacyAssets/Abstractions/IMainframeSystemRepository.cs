using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;

/// <summary>
/// Repositório de sistemas mainframe do catálogo legacy.
/// </summary>
public interface IMainframeSystemRepository
{
    Task<MainframeSystem?> GetByIdAsync(MainframeSystemId id, CancellationToken cancellationToken);
    Task<MainframeSystem?> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task<IReadOnlyList<MainframeSystem>> ListAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MainframeSystem>> ListFilteredAsync(
        string? teamName, string? domain, Criticality? criticality,
        LifecycleStatus? lifecycleStatus, string? searchTerm,
        CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
    void Add(MainframeSystem system);
}
