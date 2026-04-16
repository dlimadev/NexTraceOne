using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

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
