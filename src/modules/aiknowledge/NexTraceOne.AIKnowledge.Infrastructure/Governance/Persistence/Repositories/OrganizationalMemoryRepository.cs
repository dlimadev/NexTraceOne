using Microsoft.EntityFrameworkCore;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class OrganizationalMemoryRepository(AiGovernanceDbContext context) : IOrganizationalMemoryRepository
{
    public async Task<OrganizationalMemoryNode?> GetByIdAsync(OrganizationalMemoryNodeId id, CancellationToken ct)
        => await context.MemoryNodes.SingleOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<OrganizationalMemoryNode>> SearchAsync(string subject, Guid tenantId, int limit, CancellationToken ct)
        => await context.MemoryNodes
            .Where(n => EF.Functions.ILike(n.Subject, $"%{subject}%") && n.TenantId == tenantId)
            .OrderByDescending(n => n.RecordedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<OrganizationalMemoryNode>> GetLinkedNodesAsync(OrganizationalMemoryNodeId id, CancellationToken ct)
    {
        var node = await context.MemoryNodes.SingleOrDefaultAsync(n => n.Id == id, ct);
        if (node is null) return [];

        var linkedIds = node.LinkedNodeIds;
        if (linkedIds.Count == 0) return [];

        var results = new List<OrganizationalMemoryNode>();
        foreach (var linkedId in linkedIds.Take(10))
        {
            var linked = await context.MemoryNodes
                .SingleOrDefaultAsync(n => n.Id == OrganizationalMemoryNodeId.From(linkedId), ct);
            if (linked is not null)
                results.Add(linked);
        }
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<OrganizationalMemoryNode>> ListByTypeAsync(string nodeType, Guid tenantId, CancellationToken ct)
        => await context.MemoryNodes
            .Where(n => n.NodeType == nodeType && n.TenantId == tenantId)
            .OrderByDescending(n => n.RecordedAt)
            .ToListAsync(ct);

    public void Add(OrganizationalMemoryNode node) => context.MemoryNodes.Add(node);
}
