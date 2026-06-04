using NexTraceOne.Catalog.Domain.Knowledge.Entities;
using NexTraceOne.Catalog.Domain.Knowledge.Enums;

namespace NexTraceOne.Catalog.Application.Knowledge.Abstractions;

/// <summary>
/// Interface do repositório de KnowledgeGraphSnapshot.
/// Define operações para snapshots do knowledge graph operacional.
/// </summary>
public interface IKnowledgeGraphSnapshotRepository
{
    Task<KnowledgeGraphSnapshot?> GetByIdAsync(KnowledgeGraphSnapshotId id, CancellationToken ct);
    Task<IReadOnlyList<KnowledgeGraphSnapshot>> ListAsync(KnowledgeGraphSnapshotStatus? status, CancellationToken ct);
    Task<KnowledgeGraphSnapshot?> GetLatestAsync(CancellationToken ct);
    void Add(KnowledgeGraphSnapshot snapshot);
    void Update(KnowledgeGraphSnapshot snapshot);
}
