using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiSkillExecutionRepository(AiGovernanceDbContext context, ICurrentTenant currentTenant) : IAiSkillExecutionRepository
{
    public async Task<AiSkillExecution?> GetByIdAsync(AiSkillExecutionId id, CancellationToken ct)
        => await context.SkillExecutions.Where(e => e.TenantId == currentTenant.Id).SingleOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<AiSkillExecution>> ListBySkillAsync(AiSkillId skillId, int limit, CancellationToken ct)
        => await context.SkillExecutions
            .Where(e => e.SkillId == skillId)
            .OrderByDescending(e => e.ExecutedAt)
            .Take(limit)
            .ToListAsync(ct);

    public void Add(AiSkillExecution execution)
        => context.SkillExecutions.Add(execution);
}
