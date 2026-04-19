using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

public interface IOrganizationalMemoryRepository
{
    Task<OrganizationalMemoryNode?> GetByIdAsync(OrganizationalMemoryNodeId id, CancellationToken ct);
    Task<IReadOnlyList<OrganizationalMemoryNode>> SearchAsync(string subject, Guid tenantId, int limit, CancellationToken ct);
    Task<IReadOnlyList<OrganizationalMemoryNode>> GetLinkedNodesAsync(OrganizationalMemoryNodeId id, CancellationToken ct);
    Task<IReadOnlyList<OrganizationalMemoryNode>> ListByTypeAsync(string nodeType, Guid tenantId, CancellationToken ct);
    void Add(OrganizationalMemoryNode node);
}
