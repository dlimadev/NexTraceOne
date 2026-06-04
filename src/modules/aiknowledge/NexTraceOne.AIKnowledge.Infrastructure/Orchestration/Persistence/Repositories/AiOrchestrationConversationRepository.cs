using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Enums;
using NexTraceOne.AIKnowledge.Infrastructure.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Repositories;

internal sealed class AiOrchestrationConversationRepository(AiHubDbContext context)
    : IAiOrchestrationConversationRepository
{
    public async Task<AiConversation?> GetByIdAsync(AiConversationId id, CancellationToken ct)
        => await context.OrchestrationConversations.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(AiConversation conversation, CancellationToken ct)
    {
        await context.OrchestrationConversations.AddAsync(conversation, ct);
        await context.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(AiConversation conversation, CancellationToken ct)
    {
        context.OrchestrationConversations.Update(conversation);
        return context.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<AiConversation> Items, int Total)> ListHistoryAsync(
        Guid? releaseId,
        string? serviceName,
        string? topicFilter,
        ConversationStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.OrchestrationConversations.AsQueryable();

        if (releaseId.HasValue)
            query = query.Where(c => c.ReleaseId == releaseId.Value);

        if (!string.IsNullOrWhiteSpace(serviceName))
            query = query.Where(c => c.ServiceName == serviceName);

        if (!string.IsNullOrWhiteSpace(topicFilter))
            query = query.Where(c => c.Topic.Contains(topicFilter));

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (from.HasValue)
            query = query.Where(c => c.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(c => c.CreatedAt <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<ConversationSummaryData>> GetRecentByReleaseAsync(
        Guid releaseId,
        int maxCount,
        CancellationToken ct)
    {
        return await context.OrchestrationConversations
            .Where(c => c.ReleaseId == releaseId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(maxCount)
            .Select(c => new ConversationSummaryData(
                c.Topic,
                c.TurnCount,
                c.Status.ToString(),
                c.Summary))
            .ToListAsync(ct);
    }
}
