using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>
/// Repositório para persistência e consulta de BurnRateSnapshot.
/// </summary>
public interface IBurnRateSnapshotRepository
{
    Task<BurnRateSnapshot?> GetLatestAsync(SloDefinitionId sloId, BurnRateWindow window, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<BurnRateSnapshot>> GetHistoryAsync(SloDefinitionId sloId, Guid tenantId, int maxCount, CancellationToken ct);
    Task AddAsync(BurnRateSnapshot snapshot, CancellationToken ct);
}
