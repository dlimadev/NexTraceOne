using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiFeedbackRepository(AiGovernanceDbContext context) : IAiFeedbackRepository
{
    public async Task AddAsync(AiFeedback feedback, CancellationToken ct)
    {
        await context.Feedbacks.AddAsync(feedback, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<AiFeedback?> GetByIdAsync(AiFeedbackId id, CancellationToken ct)
        => await context.Feedbacks.SingleOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<AiFeedback>> ListByConversationIdAsync(Guid conversationId, CancellationToken ct)
        => await context.Feedbacks.Where(f => f.ConversationId == conversationId).OrderByDescending(f => f.SubmittedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<AiFeedback>> ListByRatingAsync(FeedbackRating rating, int limit, CancellationToken ct)
        => await context.Feedbacks.Where(f => f.Rating == rating).OrderByDescending(f => f.SubmittedAt).Take(limit).ToListAsync(ct);

    public async Task<int> CountByRatingAsync(FeedbackRating rating, CancellationToken ct)
        => await context.Feedbacks.CountAsync(f => f.Rating == rating, ct);

    public async Task<IReadOnlyList<AiFeedback>> ListByAgentNameAsync(string agentName, int limit, CancellationToken ct)
        => await context.Feedbacks.Where(f => f.AgentName == agentName).OrderByDescending(f => f.SubmittedAt).Take(limit).ToListAsync(ct);

    public async Task<int> CountNegativeSinceAsync(
        string agentName,
        string modelUsed,
        DateTimeOffset since,
        CancellationToken ct)
        => await context.Feedbacks.CountAsync(
            f => f.AgentName == agentName
                 && f.ModelUsed == modelUsed
                 && f.Rating == FeedbackRating.Negative
                 && f.SubmittedAt >= since,
            ct);

    public async Task<double> GetAverageRatingAsync(Guid agentId, CancellationToken ct)
    {
        // Obtém execuções do agent e correlaciona com feedbacks via AgentExecutionId
        var typedAgentId = AiAgentId.From(agentId);
        var executionIds = await context.AgentExecutions
            .Where(e => e.AgentId == typedAgentId)
            .Select(e => (Guid?)e.Id.Value)
            .ToListAsync(ct);

        if (executionIds.Count == 0)
            return 0.0;

        var ratings = await context.Feedbacks
            .Where(f => f.AgentExecutionId.HasValue && executionIds.Contains(f.AgentExecutionId))
            .Select(f => (int)f.Rating)
            .ToListAsync(ct);

        return ratings.Count == 0 ? 0.0 : ratings.Average();
    }
}
