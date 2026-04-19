using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

public interface ISelfHealingActionRepository
{
    Task<SelfHealingAction?> GetByIdAsync(SelfHealingActionId id, CancellationToken ct);
    Task<IReadOnlyList<SelfHealingAction>> ListByIncidentAsync(string incidentId, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<SelfHealingAction>> ListPendingApprovalAsync(Guid tenantId, CancellationToken ct);
    void Add(SelfHealingAction action);
}
