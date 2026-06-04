using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.AIKnowledge.Infrastructure.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Repositories;

internal sealed class KnowledgeCaptureEntryRepository(AiHubDbContext context)
    : IKnowledgeCaptureEntryRepository
{
    public async Task<KnowledgeCaptureEntry?> GetByIdAsync(KnowledgeCaptureEntryId id, CancellationToken ct)
        => await context.KnowledgeCaptureEntries.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddAsync(KnowledgeCaptureEntry entry, CancellationToken ct)
    {
        await context.KnowledgeCaptureEntries.AddAsync(entry, ct);
        await context.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(KnowledgeCaptureEntry entry, CancellationToken ct)
    {
        context.KnowledgeCaptureEntries.Update(entry);
        return context.SaveChangesAsync(ct);
    }

    public async Task<bool> HasDuplicateTitleInConversationAsync(
        AiConversationId conversationId,
        KnowledgeCaptureEntryId excludeId,
        string title,
        CancellationToken ct)
    {
        return await context.KnowledgeCaptureEntries.AnyAsync(
            e => e.ConversationId == conversationId &&
                 e.Id != excludeId &&
                 e.Title == title,
            ct);
    }
}
