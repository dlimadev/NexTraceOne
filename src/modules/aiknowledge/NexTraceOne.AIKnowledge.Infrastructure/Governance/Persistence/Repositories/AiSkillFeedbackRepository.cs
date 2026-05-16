using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiSkillFeedbackRepository(AiGovernanceDbContext context, ICurrentTenant currentTenant) : IAiSkillFeedbackRepository
{
    public async Task<IReadOnlyList<AiSkillFeedback>> ListByExecutionAsync(
        AiSkillExecutionId executionId, CancellationToken ct)
        => await context.SkillFeedbacks
            .Where(f => f.SkillExecutionId == executionId)
            .OrderByDescending(f => f.SubmittedAt)
            .ToListAsync(ct);

    public async Task<double?> GetAverageRatingBySkillAsync(AiSkillId skillId, CancellationToken ct)
    {
        var executionIds = await context.SkillExecutions
            .Where(e => e.SkillId == skillId)
            .Select(e => e.Id)
            .ToListAsync(ct);

        if (!executionIds.Any())
            return null;

        var ratings = await context.SkillFeedbacks
            .Where(f => executionIds.Contains(f.SkillExecutionId))
            .Select(f => (double)f.Rating)
            .ToListAsync(ct);

        return ratings.Count == 0 ? null : ratings.Average();
    }

    public void Add(AiSkillFeedback feedback)
        => context.SkillFeedbacks.Add(feedback);
}
