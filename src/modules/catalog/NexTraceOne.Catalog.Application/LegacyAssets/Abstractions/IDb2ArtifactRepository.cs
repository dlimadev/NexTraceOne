using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;

/// <summary>
/// Repositório de artefactos DB2 do catálogo legacy.
/// </summary>
public interface IDb2ArtifactRepository
{
    Task<Db2Artifact?> GetByIdAsync(Db2ArtifactId id, CancellationToken cancellationToken);
    Task<Db2Artifact?> GetByNameAndSystemAsync(string name, MainframeSystemId systemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Db2Artifact>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken);
    void Add(Db2Artifact artifact);
}
