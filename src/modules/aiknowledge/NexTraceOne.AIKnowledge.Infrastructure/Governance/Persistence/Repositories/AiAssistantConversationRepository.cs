using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiAssistantConversationRepository(AiGovernanceDbContext context) : IAiAssistantConversationRepository
{
    public async Task<IReadOnlyList<AiAssistantConversation>> ListAsync(
        string? userId, bool? isActive, int pageSize, CancellationToken ct)
    {
        var query = context.Conversations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(c => c.CreatedBy == userId);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        return await query
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ThenByDescending(c => c.CreatedAt)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<AiAssistantConversation?> GetByIdAsync(AiAssistantConversationId id, CancellationToken ct)
        => await context.Conversations.SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(AiAssistantConversation conversation, CancellationToken ct)
    {
        await context.Conversations.AddAsync(conversation, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AiAssistantConversation conversation, CancellationToken ct)
    {
        context.Conversations.Update(conversation);
        await context.SaveChangesAsync(ct);
    }

    public async Task<int> CountAsync(string? userId, bool? isActive, CancellationToken ct)
    {
        var query = context.Conversations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(c => c.CreatedBy == userId);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        return await query.CountAsync(ct);
    }
}
