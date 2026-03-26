using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;

/// <summary>
/// Repositório para persistência e consulta de ErrorBudgetSnapshot.
/// </summary>
public interface IErrorBudgetSnapshotRepository
{
    Task<ErrorBudgetSnapshot?> GetLatestAsync(SloDefinitionId sloId, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<ErrorBudgetSnapshot>> GetHistoryAsync(SloDefinitionId sloId, Guid tenantId, int maxCount, CancellationToken ct);
    Task AddAsync(ErrorBudgetSnapshot snapshot, CancellationToken ct);
}
