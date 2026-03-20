using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiAccessPolicyRepository(AiGovernanceDbContext context) : IAiAccessPolicyRepository
{
    public async Task<IReadOnlyList<AIAccessPolicy>> ListAsync(string? scope, bool? isActive, CancellationToken ct)
    {
        var query = context.AccessPolicies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(scope))
            query = query.Where(p => p.Scope == scope);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        return await query.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<AIAccessPolicy?> GetByIdAsync(AIAccessPolicyId id, CancellationToken ct)
        => await context.AccessPolicies.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(AIAccessPolicy policy, CancellationToken ct)
        => await context.AccessPolicies.AddAsync(policy, ct);

    public Task UpdateAsync(AIAccessPolicy policy, CancellationToken ct)
    {
        context.AccessPolicies.Update(policy);
        return Task.CompletedTask;
    }
}

internal sealed class AiModelRepository(AiGovernanceDbContext context) : IAiModelRepository
{
    public async Task<IReadOnlyList<AIModel>> ListAsync(
        string? provider, ModelType? modelType, ModelStatus? status, bool? isInternal, CancellationToken ct)
    {
        var query = context.Models.AsQueryable();

        if (!string.IsNullOrWhiteSpace(provider))
            query = query.Where(m => m.Provider == provider);

        if (modelType.HasValue)
            query = query.Where(m => m.ModelType == modelType.Value);

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        if (isInternal.HasValue)
            query = query.Where(m => m.IsInternal == isInternal.Value);

        return await query.OrderBy(m => m.Name).ToListAsync(ct);
    }

    public async Task<AIModel?> GetByIdAsync(AIModelId id, CancellationToken ct)
        => await context.Models.SingleOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddAsync(AIModel model, CancellationToken ct)
        => await context.Models.AddAsync(model, ct);

    public Task UpdateAsync(AIModel model, CancellationToken ct)
    {
        context.Models.Update(model);
        return Task.CompletedTask;
    }
}

internal sealed class AiBudgetRepository(AiGovernanceDbContext context) : IAiBudgetRepository
{
    public async Task<IReadOnlyList<AIBudget>> ListAsync(string? scope, bool? isActive, CancellationToken ct)
    {
        var query = context.Budgets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(scope))
            query = query.Where(b => b.Scope == scope);

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive.Value);

        return await query.OrderBy(b => b.Name).ToListAsync(ct);
    }

    public async Task<AIBudget?> GetByIdAsync(AIBudgetId id, CancellationToken ct)
        => await context.Budgets.SingleOrDefaultAsync(b => b.Id == id, ct);

    public Task UpdateAsync(AIBudget budget, CancellationToken ct)
    {
        context.Budgets.Update(budget);
        return Task.CompletedTask;
    }
}

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
            .OrderByDescending(c => c.LastMessageAt)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<AiAssistantConversation?> GetByIdAsync(AiAssistantConversationId id, CancellationToken ct)
    {
        var conversations = await context.Conversations.ToListAsync(ct);
        return conversations.SingleOrDefault(c => c.Id.Value == id.Value);
    }

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

internal sealed class AiMessageRepository(AiGovernanceDbContext context) : IAiMessageRepository
{
    public async Task<IReadOnlyList<AiMessage>> ListByConversationAsync(
        Guid conversationId, int pageSize, CancellationToken ct)
    {
        var messages = await context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Timestamp)
            .Take(pageSize)
            .ToListAsync(ct);

        return messages
            .OrderBy(m => m.Timestamp)
            .ToList();
    }

    public async Task AddAsync(AiMessage message, CancellationToken ct)
    {
        await context.Messages.AddAsync(message, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<int> CountByConversationAsync(Guid conversationId, CancellationToken ct)
        => await context.Messages.CountAsync(m => m.ConversationId == conversationId, ct);
}

internal sealed class AiUsageEntryRepository(AiGovernanceDbContext context) : IAiUsageEntryRepository
{
    public async Task<IReadOnlyList<AIUsageEntry>> ListAsync(
        string? userId, Guid? modelId, DateTimeOffset? startDate, DateTimeOffset? endDate,
        UsageResult? result, AIClientType? clientType, int pageSize, CancellationToken ct)
    {
        var query = context.UsageEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(e => e.UserId == userId);

        if (modelId.HasValue)
            query = query.Where(e => e.ModelId == modelId.Value);

        if (startDate.HasValue)
            query = query.Where(e => e.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.Timestamp <= endDate.Value);

        if (result.HasValue)
            query = query.Where(e => e.Result == result.Value);

        if (clientType.HasValue)
            query = query.Where(e => e.ClientType == clientType.Value);

        return await query
            .OrderByDescending(e => e.Timestamp)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AIUsageEntry entry, CancellationToken ct)
    {
        await context.UsageEntries.AddAsync(entry, ct);
        await context.SaveChangesAsync(ct);
    }
}

internal sealed class AiKnowledgeSourceRepository(AiGovernanceDbContext context) : IAiKnowledgeSourceRepository
{
    public async Task<IReadOnlyList<AIKnowledgeSource>> ListAsync(
        KnowledgeSourceType? sourceType, bool? isActive, CancellationToken ct)
    {
        var query = context.KnowledgeSources.AsQueryable();

        if (sourceType.HasValue)
            query = query.Where(s => s.SourceType == sourceType.Value);

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        return await query.OrderBy(s => s.Priority).ToListAsync(ct);
    }
}

internal sealed class AiIdeClientRegistrationRepository(AiGovernanceDbContext context) : IAiIdeClientRegistrationRepository
{
    public async Task<AIIDEClientRegistration?> GetByIdAsync(AIIDEClientRegistrationId id, CancellationToken cancellationToken)
        => await context.IdeClientRegistrations.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AIIDEClientRegistration>> ListAsync(
        string? userId, AIClientType? clientType, bool? isActive, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.IdeClientRegistrations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(r => r.UserId == userId);

        if (clientType.HasValue)
            query = query.Where(r => r.ClientType == clientType.Value);

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        return await query.Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AIIDEClientRegistration registration, CancellationToken cancellationToken)
        => await context.IdeClientRegistrations.AddAsync(registration, cancellationToken);

    public Task UpdateAsync(AIIDEClientRegistration registration, CancellationToken cancellationToken)
    {
        context.IdeClientRegistrations.Update(registration);
        return Task.CompletedTask;
    }
}

internal sealed class AiIdeCapabilityPolicyRepository(AiGovernanceDbContext context) : IAiIdeCapabilityPolicyRepository
{
    public async Task<AIIDECapabilityPolicy?> GetByIdAsync(AIIDECapabilityPolicyId id, CancellationToken cancellationToken)
        => await context.IdeCapabilityPolicies.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<AIIDECapabilityPolicy?> GetByClientTypeAndPersonaAsync(
        AIClientType clientType, string? persona, CancellationToken cancellationToken)
        => await context.IdeCapabilityPolicies
            .Where(p => p.ClientType == clientType && p.Persona == persona && p.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AIIDECapabilityPolicy>> ListAsync(
        AIClientType? clientType, bool? isActive, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.IdeCapabilityPolicies.AsQueryable();

        if (clientType.HasValue)
            query = query.Where(p => p.ClientType == clientType.Value);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        return await query.Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AIIDECapabilityPolicy policy, CancellationToken cancellationToken)
        => await context.IdeCapabilityPolicies.AddAsync(policy, cancellationToken);

    public Task UpdateAsync(AIIDECapabilityPolicy policy, CancellationToken cancellationToken)
    {
        context.IdeCapabilityPolicies.Update(policy);
        return Task.CompletedTask;
    }
}

internal sealed class AiRoutingDecisionRepository(AiGovernanceDbContext context) : IAiRoutingDecisionRepository
{
    public async Task<AIRoutingDecision?> GetByIdAsync(AIRoutingDecisionId id, CancellationToken ct)
        => await context.RoutingDecisions.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<AIRoutingDecision?> GetByCorrelationIdAsync(string correlationId, CancellationToken ct)
        => await context.RoutingDecisions.SingleOrDefaultAsync(d => d.CorrelationId == correlationId, ct);

    public async Task AddAsync(AIRoutingDecision decision, CancellationToken ct)
        => await context.RoutingDecisions.AddAsync(decision, ct);

    public async Task<IReadOnlyList<AIRoutingDecision>> ListRecentAsync(int pageSize, CancellationToken ct)
        => await context.RoutingDecisions
            .OrderByDescending(d => d.DecidedAt)
            .Take(pageSize)
            .ToListAsync(ct);
}

internal sealed class AiRoutingStrategyRepository(AiGovernanceDbContext context) : IAiRoutingStrategyRepository
{
    public async Task<IReadOnlyList<AIRoutingStrategy>> ListAsync(bool? isActive, CancellationToken ct)
    {
        var query = context.RoutingStrategies.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        return await query.OrderBy(s => s.Priority).ToListAsync(ct);
    }

    public async Task<AIRoutingStrategy?> GetByIdAsync(AIRoutingStrategyId id, CancellationToken ct)
        => await context.RoutingStrategies.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(AIRoutingStrategy strategy, CancellationToken ct)
        => await context.RoutingStrategies.AddAsync(strategy, ct);
}
