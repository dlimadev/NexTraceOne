using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

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
