using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>Repositório de registos de alertas disparados.</summary>
public interface IAlertFiringRecordRepository
{
    Task<IReadOnlyList<AlertFiringRecord>> ListByTenantAsync(
        Guid tenantId,
        AlertFiringStatus? statusFilter,
        int days,
        CancellationToken ct = default);

    Task<AlertFiringRecord?> GetByIdAsync(AlertFiringRecordId id, CancellationToken ct = default);
    Task<bool> HasFiringAlertAsync(Guid tenantId, Guid alertRuleId, CancellationToken ct = default);
    void Add(AlertFiringRecord record);
    void Update(AlertFiringRecord record);
}
