using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Application.Portal.Abstractions;

/// <summary>
/// Repositório de políticas de rate limiting por API.
/// </summary>
public interface IApiRateLimitPolicyRepository
{
    Task<RateLimitPolicy?> GetByApiAssetIdAsync(Guid apiAssetId, CancellationToken ct = default);
    void Add(RateLimitPolicy policy);
    void Update(RateLimitPolicy policy);
}
