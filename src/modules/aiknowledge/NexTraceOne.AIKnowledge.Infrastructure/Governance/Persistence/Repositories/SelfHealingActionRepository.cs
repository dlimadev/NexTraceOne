using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class SelfHealingActionRepository(AiGovernanceDbContext context, ICurrentTenant currentTenant) : ISelfHealingActionRepository
{
    public async Task<SelfHealingAction?> GetByIdAsync(SelfHealingActionId id, CancellationToken ct)
        => await context.SelfHealingActions.Where(e => e.TenantId == currentTenant.Id).SingleOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<SelfHealingAction>> ListByIncidentAsync(string incidentId, Guid tenantId, CancellationToken ct)
        => await context.SelfHealingActions
            .Where(a => a.IncidentId == incidentId && a.TenantId == tenantId)
            .OrderByDescending(a => a.ProposedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SelfHealingAction>> ListPendingApprovalAsync(Guid tenantId, CancellationToken ct)
        => await context.SelfHealingActions
            .Where(a => a.TenantId == tenantId && a.Status == "pending")
            .OrderByDescending(a => a.Confidence)
            .ToListAsync(ct);

    public void Add(SelfHealingAction action) => context.SelfHealingActions.Add(action);
}
