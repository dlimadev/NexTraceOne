using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiExecutionPlanRepository(AiGovernanceDbContext context) : IAiExecutionPlanRepository
{
    public async Task AddAsync(AIExecutionPlan plan, CancellationToken ct)
        => await context.ExecutionPlans.AddAsync(plan, ct);

    public async Task<AIExecutionPlan?> GetByIdAsync(AIExecutionPlanId id, CancellationToken ct)
        => await context.ExecutionPlans.SingleOrDefaultAsync(p => p.Id == id, ct);
}
