using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Repositories;

internal sealed class AgentWorkflowExecutionRepository(AiOrchestrationDbContext context)
    : IAgentWorkflowExecutionRepository
{
    public async Task<AgentWorkflowExecution?> GetByIdAsync(AgentWorkflowExecutionId id, CancellationToken ct)
        => await context.WorkflowExecutions.SingleOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<AgentWorkflowExecution>> ListByWorkflowAsync(
        string workflowName, int page, int pageSize, CancellationToken ct)
        => await context.WorkflowExecutions
            .Where(e => e.WorkflowName == workflowName)
            .OrderByDescending(e => e.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AgentWorkflowExecution>> ListRecentAsync(
        int page, int pageSize, CancellationToken ct)
        => await context.WorkflowExecutions
            .OrderByDescending(e => e.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AgentWorkflowExecution>> ListByCallerTeamAsync(
        string callerTeamId, int page, int pageSize, CancellationToken ct)
        => await context.WorkflowExecutions
            .Where(e => e.CallerTeamId == callerTeamId)
            .OrderByDescending(e => e.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task AddAsync(AgentWorkflowExecution execution, CancellationToken ct)
        => await context.WorkflowExecutions.AddAsync(execution, ct);

    public Task UpdateAsync(AgentWorkflowExecution execution, CancellationToken ct)
    {
        context.WorkflowExecutions.Update(execution);
        return Task.CompletedTask;
    }
}
