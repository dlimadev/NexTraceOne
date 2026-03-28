using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Contracts.Orchestration.ServiceInterfaces;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Services;

internal sealed class AiOrchestrationModule(
    AiOrchestrationDbContext context) : IAiOrchestrationModule
{
    // TODO(P02.6-followup): replace placeholders when orchestration persists token/model attribution metadata.
    private const int UnknownTokensUsed = 0;
    private const string? UnknownModelUsed = null;

    public async Task<ConversationSummaryDto?> GetConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        return await context.Conversations
            .AsNoTracking()
            .Where(c => c.Id == Domain.Orchestration.Entities.AiConversationId.From(conversationId))
            .Select(c => new ConversationSummaryDto(
                c.Id.Value,
                c.Topic,
                c.StartedBy,
                c.ServiceName,
                c.TurnCount,
                UnknownTokensUsed,
                UnknownModelUsed,
                c.CreatedAt,
                c.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsByServiceAsync(
        string serviceName,
        int limit = 10,
        CancellationToken ct = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 100);

        return await context.Conversations
            .AsNoTracking()
            .Where(c => c.ServiceName == serviceName)
            .OrderByDescending(c => c.StartedAt)
            .Take(safeLimit)
            .Select(c => new ConversationSummaryDto(
                c.Id.Value,
                c.Topic,
                c.StartedBy,
                c.ServiceName,
                c.TurnCount,
                UnknownTokensUsed,
                UnknownModelUsed,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<AgentExecutionResultDto?> GetAgentExecutionResultAsync(Guid executionId, CancellationToken ct = default)
    {
        var artifact = await context.TestArtifacts
            .AsNoTracking()
            .Where(a => a.Id == Domain.Orchestration.Entities.GeneratedTestArtifactId.From(executionId))
            .Select(a => new AgentExecutionResultDto(
                a.Id.Value,
                $"test-generation:{a.TestFramework}",
                a.ServiceName,
                a.Status.ToString(),
                $"Generated test artifact for release {a.ReleaseId}",
                a.GeneratedAt,
                UnknownTokensUsed))
            .FirstOrDefaultAsync(ct);

        if (artifact is not null)
            return artifact;

        var conversation = await context.Conversations
            .AsNoTracking()
            .Where(c => c.Id == Domain.Orchestration.Entities.AiConversationId.From(executionId))
            .Select(c => new AgentExecutionResultDto(
                c.Id.Value,
                "conversation-analysis",
                c.ServiceName,
                c.Status.ToString(),
                c.Summary ?? $"Conversation on {c.Topic}",
                c.StartedAt,
                UnknownTokensUsed))
            .FirstOrDefaultAsync(ct);

        if (conversation is not null)
            return conversation;

        var contextSummary = await context.Contexts
            .AsNoTracking()
            .Where(c => c.Id == Domain.Orchestration.Entities.AiContextId.From(executionId))
            .Select(c => new AgentExecutionResultDto(
                c.Id.Value,
                c.ContextType,
                c.ServiceName,
                "Completed",
                $"AI context assembled for {c.ContextType}",
                c.AssembledAt,
                c.TokenEstimate))
            .FirstOrDefaultAsync(ct);

        return contextSummary;
    }
}
