using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;

/// <summary>
/// Implementação EF Core do repositório de LegacyDependency.
/// </summary>
internal sealed class LegacyDependencyRepository(LegacyAssetsDbContext dbContext) : ILegacyDependencyRepository
{
    public async Task<IReadOnlyList<LegacyDependency>> ListBySourceAsync(Guid sourceAssetId, CancellationToken ct)
        => await dbContext.LegacyDependencies
            .AsNoTracking()
            .Where(d => d.SourceAssetId == sourceAssetId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LegacyDependency>> ListByTargetAsync(Guid targetAssetId, CancellationToken ct)
        => await dbContext.LegacyDependencies
            .AsNoTracking()
            .Where(d => d.TargetAssetId == targetAssetId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LegacyDependency>> ListBySourceTypeAsync(MainframeAssetType sourceType, CancellationToken ct)
        => await dbContext.LegacyDependencies
            .AsNoTracking()
            .Where(d => d.SourceAssetType == sourceType)
            .ToListAsync(ct);

    public async Task AddAsync(LegacyDependency dependency, CancellationToken ct)
        => await dbContext.LegacyDependencies.AddAsync(dependency, ct);
}
