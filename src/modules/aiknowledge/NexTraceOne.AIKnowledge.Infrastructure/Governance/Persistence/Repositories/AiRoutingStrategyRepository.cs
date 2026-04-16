using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

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

    public Task UpdateAsync(AIRoutingStrategy strategy, CancellationToken ct)
    {
        context.RoutingStrategies.Update(strategy);
        return Task.CompletedTask;
    }
}
