using Microsoft.EntityFrameworkCore;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class ChangeConfidenceRepository(AiGovernanceDbContext context) : IAiChangeConfidenceRepository
{
    public async Task<ChangeConfidenceScore?> GetByChangeIdAsync(string changeId, Guid tenantId, CancellationToken ct)
        => await context.ChangeConfidenceScores
            .Where(s => s.ChangeId == changeId && s.TenantId == tenantId)
            .OrderByDescending(s => s.CalculatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ChangeConfidenceScore>> ListByServiceAsync(string serviceName, Guid tenantId, int limit, CancellationToken ct)
        => await context.ChangeConfidenceScores
            .Where(s => s.ServiceName == serviceName && s.TenantId == tenantId)
            .OrderByDescending(s => s.CalculatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public void Add(ChangeConfidenceScore score) => context.ChangeConfidenceScores.Add(score);
}
