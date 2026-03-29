using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;

/// <summary>
/// Repositório de copybooks COBOL do catálogo legacy.
/// </summary>
public interface ICopybookRepository
{
    Task<Copybook?> GetByIdAsync(CopybookId id, CancellationToken cancellationToken);
    Task<Copybook?> GetByNameAndSystemAsync(string name, MainframeSystemId systemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Copybook>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Copybook>> SearchAsync(string searchTerm, CancellationToken cancellationToken);
    void Add(Copybook copybook);
}
