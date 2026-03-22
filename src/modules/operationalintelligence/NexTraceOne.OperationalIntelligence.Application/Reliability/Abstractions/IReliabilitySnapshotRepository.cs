using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>
/// Repositório para persistência de ReliabilitySnapshot.
/// </summary>
public interface IReliabilitySnapshotRepository
{
    Task<IReadOnlyList<ReliabilitySnapshot>> GetHistoryAsync(string serviceId, Guid tenantId, int maxCount, CancellationToken ct);
    Task AddAsync(ReliabilitySnapshot snapshot, CancellationToken ct);
    Task<ReliabilitySnapshot?> GetLatestAsync(string serviceId, Guid tenantId, CancellationToken ct);
}
