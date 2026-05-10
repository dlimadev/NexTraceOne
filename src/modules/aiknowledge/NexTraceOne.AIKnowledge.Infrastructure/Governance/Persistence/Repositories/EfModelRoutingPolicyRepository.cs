using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class EfModelRoutingPolicyRepository(AiGovernanceDbContext context) : IModelRoutingPolicyRepository
{
    public async Task<ModelRoutingPolicy?> GetActiveAsync(Guid tenantId, PromptIntent intent, CancellationToken ct)
        => await context.ModelRoutingPolicies
            .Where(p => p.TenantId == tenantId && p.Intent == intent && p.IsActive)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ModelRoutingPolicy>> ListByTenantAsync(Guid tenantId, CancellationToken ct)
        => await context.ModelRoutingPolicies
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Intent)
            .ToListAsync(ct);

    public async Task AddAsync(ModelRoutingPolicy policy, CancellationToken ct)
        => await context.ModelRoutingPolicies.AddAsync(policy, ct);
}
