using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiAgentTrajectoryFeedbackRepository(AiGovernanceDbContext context)
    : IAiAgentTrajectoryFeedbackRepository
{
    public async Task<AiAgentTrajectoryFeedback?> GetByIdAsync(
        AiAgentTrajectoryFeedbackId id, CancellationToken ct)
        => await context.AgentTrajectoryFeedbacks
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<AiAgentTrajectoryFeedback>> ListPendingExportAsync(
        int limit, CancellationToken ct)
        => await context.AgentTrajectoryFeedbacks
            .Where(f => !f.ExportedForTraining)
            .OrderBy(f => f.SubmittedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<bool> ExistsByExecutionIdAsync(
        AiAgentExecutionId executionId, CancellationToken ct)
        => await context.AgentTrajectoryFeedbacks
            .AnyAsync(f => f.ExecutionId == executionId, ct);

    public void Add(AiAgentTrajectoryFeedback feedback)
        => context.AgentTrajectoryFeedbacks.Add(feedback);
}
