using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Contracts.Governance.ServiceInterfaces;
using NexTraceOne.AIKnowledge.Contracts.Orchestration.ServiceInterfaces;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Services;

internal sealed class AiOrchestrationModule(
    AiOrchestrationDbContext context,
    IAiGovernanceModule governanceModule) : IAiOrchestrationModule
{
    public async Task<ConversationSummaryDto?> GetConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        var conversation = await context.Conversations
            .AsNoTracking()
            .Where(c => c.Id == Domain.Orchestration.Entities.AiConversationId.From(conversationId))
            .Select(c => new
            {
                c.Id,
                c.Topic,
                c.StartedBy,
                c.ServiceName,
                c.TurnCount,
                c.CreatedAt,
                c.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (conversation is null)
            return null;

        var usage = await governanceModule.GetTokenUsageByExecutionIdAsync(
            conversationId.ToString(), ct);

        return new ConversationSummaryDto(
            conversation.Id.Value,
            conversation.Topic,
            conversation.StartedBy,
            conversation.ServiceName,
            conversation.TurnCount,
            usage?.TotalTokens ?? 0,
            usage?.ModelName,
            conversation.CreatedAt,
            conversation.UpdatedAt);
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsByServiceAsync(
        string serviceName,
        int limit = 10,
        CancellationToken ct = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 100);

        var conversations = await context.Conversations
            .AsNoTracking()
            .Where(c => c.ServiceName == serviceName)
            .OrderByDescending(c => c.StartedAt)
            .Take(safeLimit)
            .Select(c => new
            {
                c.Id,
                c.Topic,
                c.StartedBy,
                c.ServiceName,
                c.TurnCount,
                c.CreatedAt,
                c.UpdatedAt
            })
            .ToListAsync(ct);

        var results = new List<ConversationSummaryDto>(conversations.Count);

        foreach (var c in conversations)
        {
            var usage = await governanceModule.GetTokenUsageByExecutionIdAsync(
                c.Id.Value.ToString(), ct);

            results.Add(new ConversationSummaryDto(
                c.Id.Value,
                c.Topic,
                c.StartedBy,
                c.ServiceName,
                c.TurnCount,
                usage?.TotalTokens ?? 0,
                usage?.ModelName,
                c.CreatedAt,
                c.UpdatedAt));
        }

        return results;
    }

    public async Task<AgentExecutionResultDto?> GetAgentExecutionResultAsync(Guid executionId, CancellationToken ct = default)
    {
        var usage = await governanceModule.GetTokenUsageByExecutionIdAsync(
            executionId.ToString(), ct);
        var tokensUsed = usage?.TotalTokens ?? 0;

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
                tokensUsed))
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
                tokensUsed))
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
                tokensUsed > 0 ? tokensUsed : c.TokenEstimate))
            .FirstOrDefaultAsync(ct);

        return contextSummary;
    }
}
