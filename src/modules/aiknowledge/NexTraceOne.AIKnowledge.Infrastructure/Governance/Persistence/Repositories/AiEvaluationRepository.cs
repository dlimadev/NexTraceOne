using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiEvaluationRepository(AiGovernanceDbContext context, ICurrentTenant currentTenant) : IAiEvaluationRepository
{
    public async Task<AiEvaluation?> GetByIdAsync(AiEvaluationId id, CancellationToken ct)
        => await context.Evaluations.Where(e => e.TenantId == currentTenant.Id).SingleOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<AiEvaluation>> GetByConversationAsync(Guid conversationId, CancellationToken ct)
        => await context.Evaluations.Where(e => e.ConversationId == conversationId).OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<AiEvaluation>> GetByAgentExecutionAsync(Guid agentExecutionId, CancellationToken ct)
        => await context.Evaluations.Where(e => e.AgentExecutionId == agentExecutionId).OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<AiEvaluation>> GetByUserAsync(string userId, CancellationToken ct)
        => await context.Evaluations.Where(e => e.UserId == userId).OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<AiEvaluation>> GetByTenantAndPeriodAsync(Guid tenantId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
        => await context.Evaluations.Where(e => e.TenantId == tenantId && e.CreatedAt >= start && e.CreatedAt <= end).OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(AiEvaluation entity, CancellationToken ct)
    {
        await context.Evaluations.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }
}
