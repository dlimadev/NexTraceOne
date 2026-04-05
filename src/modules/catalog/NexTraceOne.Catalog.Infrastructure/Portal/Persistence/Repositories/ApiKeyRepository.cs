using Microsoft.EntityFrameworkCore;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Repositories;

internal sealed class ApiKeyRepository(DeveloperPortalDbContext context) : IApiKeyRepository
{
    public async Task<ApiKey?> GetByIdAsync(ApiKeyId id, CancellationToken ct = default)
        => await context.ApiKeys.SingleOrDefaultAsync(k => k.Id == id, ct);

    public async Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct = default)
        => await context.ApiKeys.SingleOrDefaultAsync(k => k.KeyHash == keyHash, ct);

    public async Task<IReadOnlyList<ApiKey>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        => await context.ApiKeys.Where(k => k.OwnerId == ownerId).OrderByDescending(k => k.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<ApiKey>> GetByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.ApiKeys.Where(k => k.ApiAssetId == apiAssetId).OrderByDescending(k => k.CreatedAt).ToListAsync(ct);

    public async Task<int> CountActiveByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        => await context.ApiKeys.CountAsync(k => k.OwnerId == ownerId && k.IsActive, ct);

    public void Add(ApiKey apiKey) => context.ApiKeys.Add(apiKey);
    public void Update(ApiKey apiKey) => context.ApiKeys.Update(apiKey);
}
