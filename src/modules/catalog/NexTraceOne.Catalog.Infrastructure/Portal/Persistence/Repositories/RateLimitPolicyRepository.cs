using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Repositories;

internal sealed class RateLimitPolicyRepository(DeveloperPortalDbContext context) : IApiRateLimitPolicyRepository
{
    public async Task<RateLimitPolicy?> GetByApiAssetIdAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.RateLimitPolicies.SingleOrDefaultAsync(p => p.ApiAssetId == apiAssetId, ct);

    public void Add(RateLimitPolicy policy) => context.RateLimitPolicies.Add(policy);
    public void Update(RateLimitPolicy policy) => context.RateLimitPolicies.Update(policy);
}
