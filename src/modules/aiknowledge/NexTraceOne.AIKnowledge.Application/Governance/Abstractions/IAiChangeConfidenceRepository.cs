using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

public interface IAiChangeConfidenceRepository
{
    Task<ChangeConfidenceScore?> GetByChangeIdAsync(string changeId, Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<ChangeConfidenceScore>> ListByServiceAsync(string serviceName, Guid tenantId, int limit, CancellationToken ct);
    void Add(ChangeConfidenceScore score);
}
