using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiTokenQuotaPolicyRepository(AiGovernanceDbContext context) : IAiTokenQuotaPolicyRepository
{
    public async Task<AiTokenQuotaPolicy?> GetByIdAsync(AiTokenQuotaPolicyId id, CancellationToken ct)
        => await context.TokenQuotaPolicies.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<AiTokenQuotaPolicy>> GetAllAsync(CancellationToken ct)
        => await context.TokenQuotaPolicies.OrderBy(p => p.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<AiTokenQuotaPolicy>> GetByScopeAsync(string scope, CancellationToken ct)
        => await context.TokenQuotaPolicies
            .Where(p => p.Scope == scope)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AiTokenQuotaPolicy>> GetForUserAsync(string userId, CancellationToken ct)
        => await context.TokenQuotaPolicies
            .Where(p => p.Scope == "user" && p.ScopeValue == userId && p.IsEnabled)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AiTokenQuotaPolicy>> GetForTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var tenantIdStr = tenantId.ToString();
        return await context.TokenQuotaPolicies
            .Where(p => p.Scope == "tenant" && p.ScopeValue == tenantIdStr && p.IsEnabled)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AiTokenQuotaPolicy entity, CancellationToken ct)
        => await context.TokenQuotaPolicies.AddAsync(entity, ct);
}
