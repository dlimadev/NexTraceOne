using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

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
