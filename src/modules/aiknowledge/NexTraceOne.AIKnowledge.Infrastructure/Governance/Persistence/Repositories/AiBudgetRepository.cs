using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

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
