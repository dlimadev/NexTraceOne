using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions;

using static NexTraceOne.Catalog.Application.DeveloperExperience.Abstractions.IIDEUsageRepository;

namespace NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de registos de uso IDE.
/// Wave AK.1 — IDE Context API.
/// </summary>
internal sealed class EfIdeUsageRepository(DeveloperExperienceDbContext context) : IIDEUsageRepository
{
    public async Task AddAsync(IdeUsageRecord record, CancellationToken ct = default)
        => await context.IdeUsageRecords.AddAsync(record, ct);

    public async Task<IReadOnlyList<IdeUsageRecord>> ListByUserAsync(
        string userId, DateTimeOffset since, CancellationToken ct = default)
        => await context.IdeUsageRecords
            .Where(r => r.UserId == userId && r.OccurredAt >= since)
            .OrderByDescending(r => r.OccurredAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<IdeUsageRecord>> ListByTenantAsync(
        string tenantId, DateTimeOffset since, CancellationToken ct = default)
        => await context.IdeUsageRecords
            .Where(r => r.TenantId == tenantId && r.OccurredAt >= since)
            .OrderByDescending(r => r.OccurredAt)
            .ToListAsync(ct);
}
