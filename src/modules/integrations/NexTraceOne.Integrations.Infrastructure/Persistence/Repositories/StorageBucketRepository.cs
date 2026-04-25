using Microsoft.EntityFrameworkCore;
using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de buckets de storage de telemetria.
/// </summary>
internal sealed class StorageBucketRepository(IntegrationsDbContext context) : IStorageBucketRepository
{
    public async Task<(IReadOnlyList<StorageBucket> Items, int TotalCount)> ListAsync(
        bool? isEnabled,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = context.StorageBuckets.AsQueryable();
        if (isEnabled.HasValue)
            query = query.Where(b => b.IsEnabled == isEnabled.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(b => b.Priority)
            .ThenByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<StorageBucket>> ListEnabledOrderedAsync(CancellationToken ct)
        => await context.StorageBuckets
            .Where(b => b.IsEnabled)
            .OrderBy(b => b.Priority)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<StorageBucket?> GetByIdAsync(StorageBucketId id, CancellationToken ct)
        => await context.StorageBuckets.SingleOrDefaultAsync(b => b.Id == id, ct);

    public async Task AddAsync(StorageBucket bucket, CancellationToken ct)
        => await context.StorageBuckets.AddAsync(bucket, ct);

    public Task UpdateAsync(StorageBucket bucket, CancellationToken ct)
    {
        context.StorageBuckets.Update(bucket);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(StorageBucket bucket, CancellationToken ct)
    {
        context.StorageBuckets.Remove(bucket);
        return Task.CompletedTask;
    }
}
