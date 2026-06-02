using NexTraceOne.AIKnowledge.Infrastructure.Persistence;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence.Repositories;

internal sealed class AiFeatureModelBindingRepository(AiHubDbContext context)
    : IAiFeatureModelBindingRepository
{
    public async Task<AiFeatureModelBinding?> GetByFeatureKeyAsync(
        string featureKey,
        Guid tenantId,
        CancellationToken ct = default)
        => await context.FeatureModelBindings
            .Where(b => b.FeatureKey == featureKey && b.TenantId == tenantId && b.IsActive)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<AiFeatureModelBinding>> ListByTenantAsync(
        Guid tenantId,
        bool? isActive,
        CancellationToken ct = default)
    {
        var query = context.FeatureModelBindings.Where(b => b.TenantId == tenantId);

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive.Value);

        return await query.OrderBy(b => b.FeatureKey).ToListAsync(ct);
    }

    public async Task<AiFeatureModelBinding?> GetByIdAsync(
        AiFeatureModelBindingId id,
        CancellationToken ct = default)
        => await context.FeatureModelBindings.SingleOrDefaultAsync(b => b.Id == id, ct);

    public async Task<bool> ExistsAsync(
        string featureKey,
        Guid tenantId,
        CancellationToken ct = default)
        => await context.FeatureModelBindings
            .AnyAsync(b => b.FeatureKey == featureKey && b.TenantId == tenantId && b.IsActive, ct);

    public async Task AddAsync(AiFeatureModelBinding binding, CancellationToken ct = default)
        => await context.FeatureModelBindings.AddAsync(binding, ct);

    public Task UpdateAsync(AiFeatureModelBinding binding, CancellationToken ct = default)
    {
        context.FeatureModelBindings.Update(binding);
        return Task.CompletedTask;
    }
}
