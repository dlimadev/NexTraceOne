using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Repositories;

internal sealed class RateLimitPolicyRepository(ServiceCatalogDbContext context) : IApiRateLimitPolicyRepository
{
    public async Task<RateLimitPolicy?> GetByApiAssetIdAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.RateLimitPolicies.SingleOrDefaultAsync(p => p.ApiAssetId == apiAssetId, ct);

    public void Add(RateLimitPolicy policy) => context.RateLimitPolicies.Add(policy);
    public void Update(RateLimitPolicy policy) => context.RateLimitPolicies.Update(policy);
}
