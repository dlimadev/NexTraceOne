using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class PromptAssetRepository(AiGovernanceDbContext context) : IPromptAssetRepository
{
    public async Task<PromptAsset?> FindByIdAsync(PromptAssetId id, CancellationToken ct = default)
        => await context.PromptAssets
            .Include(a => a.Versions)
            .SingleOrDefaultAsync(a => a.Id == id, ct);

    public async Task<PromptAsset?> FindBySlugAsync(string slug, Guid? tenantId, CancellationToken ct = default)
        => await context.PromptAssets
            .Include(a => a.Versions)
            .Where(a => a.Slug == slug && a.TenantId == tenantId)
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<PromptAsset>> ListAsync(
        Guid? tenantId, string? category, bool activeOnly, CancellationToken ct = default)
    {
        var query = context.PromptAssets.AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(a => a.TenantId == tenantId.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(a => a.Category == category);

        if (activeOnly)
            query = query.Where(a => a.IsActive);

        return await query.OrderBy(a => a.Name).ToListAsync(ct);
    }

    public async Task AddAsync(PromptAsset asset, CancellationToken ct = default)
        => await context.PromptAssets.AddAsync(asset, ct);

    public async Task<bool> SlugExistsAsync(string slug, Guid? tenantId, CancellationToken ct = default)
        => await context.PromptAssets
            .AnyAsync(a => a.Slug == slug && a.TenantId == tenantId, ct);
}
