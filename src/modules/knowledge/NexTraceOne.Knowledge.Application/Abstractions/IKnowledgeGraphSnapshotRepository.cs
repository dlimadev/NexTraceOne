using NexTraceOne.Knowledge.Domain.Entities;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Application.Abstractions;

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
