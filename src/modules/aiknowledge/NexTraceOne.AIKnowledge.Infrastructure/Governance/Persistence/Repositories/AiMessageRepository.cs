using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiMessageRepository(AiGovernanceDbContext context) : IAiMessageRepository
{
    public async Task<IReadOnlyList<AiMessage>> ListByConversationAsync(
        Guid conversationId, int pageSize, CancellationToken ct)
    {
        var messages = await context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Timestamp)
            .ThenByDescending(m => m.CreatedAt)
            .Take(pageSize)
            .ToListAsync(ct);

        return messages
            .OrderBy(m => m.Timestamp)
            .ThenBy(m => m.CreatedAt)
            .ToList();
    }

    public async Task AddAsync(AiMessage message, CancellationToken ct)
    {
        await context.Messages.AddAsync(message, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<int> CountByConversationAsync(Guid conversationId, CancellationToken ct)
        => await context.Messages.CountAsync(m => m.ConversationId == conversationId, ct);

    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct)
        => await context.Messages.Where(m => m.CreatedAt < cutoff).ExecuteDeleteAsync(ct);
}
