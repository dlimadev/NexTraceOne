using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class EfAgentExecutionPlanRepository(AiGovernanceDbContext context) : IAgentExecutionPlanRepository
{
    public async Task<AgentExecutionPlan?> GetByIdAsync(AgentExecutionPlanId id, CancellationToken ct)
        => await context.AgentExecutionPlans
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<AgentExecutionPlan>> ListByTenantAsync(
        Guid tenantId,
        PlanStatus? statusFilter,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.AgentExecutionPlans
            .Where(p => p.TenantId == tenantId);

        if (statusFilter.HasValue)
            query = query.Where(p => p.PlanStatus == statusFilter.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AgentExecutionPlan plan, CancellationToken ct)
        => await context.AgentExecutionPlans.AddAsync(plan, ct);

    public Task UpdateAsync(AgentExecutionPlan plan, CancellationToken ct)
    {
        context.AgentExecutionPlans.Update(plan);
        return Task.CompletedTask;
    }
}
