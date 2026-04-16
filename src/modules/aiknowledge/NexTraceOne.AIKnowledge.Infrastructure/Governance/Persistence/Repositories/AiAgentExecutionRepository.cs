using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiAgentExecutionRepository(AiGovernanceDbContext context) : IAiAgentExecutionRepository
{
    public async Task<AiAgentExecution?> GetByIdAsync(AiAgentExecutionId id, CancellationToken ct)
        => await context.AgentExecutions.SingleOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<AiAgentExecution>> ListByAgentAsync(
        AiAgentId agentId, int pageSize, CancellationToken ct)
        => await context.AgentExecutions
            .Where(e => e.AgentId == agentId)
            .OrderByDescending(e => e.StartedAt)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AiAgentExecution>> ListByUserAsync(
        string userId, int pageSize, CancellationToken ct)
        => await context.AgentExecutions
            .Where(e => e.ExecutedBy == userId)
            .OrderByDescending(e => e.StartedAt)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task AddAsync(AiAgentExecution execution, CancellationToken ct)
        => await context.AgentExecutions.AddAsync(execution, ct);

    public Task UpdateAsync(AiAgentExecution execution, CancellationToken ct)
    {
        context.AgentExecutions.Update(execution);
        return Task.CompletedTask;
    }
}
