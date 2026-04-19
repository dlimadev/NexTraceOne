using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

public interface IGuardianAlertRepository
{
    Task<IReadOnlyList<GuardianAlert>> ListOpenAsync(Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<GuardianAlert>> ListByServiceAsync(string serviceName, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<GuardianAlert>> ListByTenantAsync(Guid tenantId, CancellationToken ct);
    Task<GuardianAlert?> GetByIdAsync(GuardianAlertId id, CancellationToken ct);
    void Add(GuardianAlert alert);
}
