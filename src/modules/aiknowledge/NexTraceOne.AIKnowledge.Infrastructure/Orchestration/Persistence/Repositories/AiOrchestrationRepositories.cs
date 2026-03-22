using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;
using NexTraceOne.AIKnowledge.Domain.Orchestration.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence.Repositories;

/// <summary>
/// Repositório de conversas multi-turno de IA do módulo de orquestração.
/// Implementa IAiOrchestrationConversationRepository com AiOrchestrationDbContext.
/// </summary>
internal sealed class AiOrchestrationConversationRepository(AiOrchestrationDbContext context)
    : IAiOrchestrationConversationRepository
{
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
        var query = context.Conversations.AsNoTracking().AsQueryable();

        if (releaseId.HasValue)
            query = query.Where(c => c.ReleaseId == releaseId.Value);

        if (!string.IsNullOrWhiteSpace(serviceName))
            query = query.Where(c => c.ServiceName.Contains(serviceName));

        if (!string.IsNullOrWhiteSpace(topicFilter))
            query = query.Where(c => c.Topic.Contains(topicFilter));

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (from.HasValue)
            query = query.Where(c => c.StartedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(c => c.StartedAt <= to.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.StartedAt)
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
        return await context.Conversations
            .AsNoTracking()
            .Where(c => c.ReleaseId == releaseId)
            .OrderByDescending(c => c.StartedAt)
            .Take(maxCount)
            .Select(c => new ConversationSummaryData(c.Topic, c.TurnCount, c.Status.ToString(), c.Summary))
            .ToListAsync(ct);
    }
}

/// <summary>
/// Repositório de entradas sugeridas para base de conhecimento da orquestração de IA.
/// </summary>
internal sealed class KnowledgeCaptureEntryRepository(AiOrchestrationDbContext context)
    : IKnowledgeCaptureEntryRepository
{
    public async Task<KnowledgeCaptureEntry?> GetByIdAsync(KnowledgeCaptureEntryId id, CancellationToken ct)
        => await context.KnowledgeCaptureEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<bool> HasDuplicateTitleInConversationAsync(
        AiConversationId conversationId,
        KnowledgeCaptureEntryId excludeId,
        string title,
        CancellationToken ct)
        => context.KnowledgeCaptureEntries.AnyAsync(e =>
            e.Id != excludeId &&
            e.ConversationId == conversationId &&
            e.Title == title, ct);
}

/// <summary>
/// Repositório de artefatos de teste gerados por IA.
/// </summary>
internal sealed class GeneratedTestArtifactRepository(AiOrchestrationDbContext context)
    : IGeneratedTestArtifactRepository
{
    public async Task AddAsync(GeneratedTestArtifact artifact, CancellationToken ct)
    {
        await context.TestArtifacts.AddAsync(artifact, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ArtifactSummaryData>> GetRecentByReleaseAsync(
        Guid releaseId,
        int maxCount,
        CancellationToken ct)
    {
        return await context.TestArtifacts
            .AsNoTracking()
            .Where(a => a.ReleaseId == releaseId)
            .OrderByDescending(a => a.GeneratedAt)
            .Take(maxCount)
            .Select(a => new ArtifactSummaryData(a.ServiceName, a.TestFramework, a.Status.ToString(), a.Confidence))
            .ToListAsync(ct);
    }
}
