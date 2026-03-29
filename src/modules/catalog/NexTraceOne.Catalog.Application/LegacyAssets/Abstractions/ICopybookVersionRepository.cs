using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;

/// <summary>
/// Repositório de versões de copybooks COBOL do catálogo legacy.
/// </summary>
public interface ICopybookVersionRepository
{
    Task<CopybookVersion?> GetByIdAsync(CopybookVersionId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CopybookVersion>> ListByCopybookAsync(CopybookId copybookId, CancellationToken cancellationToken);
    void Add(CopybookVersion version);
}
