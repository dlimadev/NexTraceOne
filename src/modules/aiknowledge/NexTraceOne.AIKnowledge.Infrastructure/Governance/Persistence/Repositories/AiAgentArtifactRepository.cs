using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiAgentArtifactRepository(AiGovernanceDbContext context) : IAiAgentArtifactRepository
{
    public async Task<AiAgentArtifact?> GetByIdAsync(AiAgentArtifactId id, CancellationToken ct)
        => await context.AgentArtifacts.SingleOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AiAgentArtifact>> ListByExecutionAsync(
        AiAgentExecutionId executionId, CancellationToken ct)
        => await context.AgentArtifacts
            .Where(a => a.ExecutionId == executionId)
            .OrderBy(a => a.Title)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AiAgentArtifact>> ListByAgentAsync(
        AiAgentId agentId, ArtifactReviewStatus? reviewStatus, int pageSize, CancellationToken ct)
    {
        var query = context.AgentArtifacts.Where(a => a.AgentId == agentId);

        if (reviewStatus.HasValue)
            query = query.Where(a => a.ReviewStatus == reviewStatus.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AiAgentArtifact artifact, CancellationToken ct)
        => await context.AgentArtifacts.AddAsync(artifact, ct);

    public Task UpdateAsync(AiAgentArtifact artifact, CancellationToken ct)
    {
        context.AgentArtifacts.Update(artifact);
        return Task.CompletedTask;
    }
}
