using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.CommercialCatalog.Application.Abstractions;
using NexTraceOne.CommercialCatalog.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Repositories;

internal sealed class FeaturePackRepository(LicensingDbContext context)
    : RepositoryBase<FeaturePack, FeaturePackId>(context), IFeaturePackRepository
{
    private readonly LicensingDbContext _context = context;

    public override async Task<FeaturePack?> GetByIdAsync(FeaturePackId id, CancellationToken ct = default)
        => await IncludeItems(_context.FeaturePacks)
            .SingleOrDefaultAsync(fp => fp.Id == id, ct);

    public async Task<FeaturePack?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => await IncludeItems(_context.FeaturePacks)
            .SingleOrDefaultAsync(fp => fp.Code == code, cancellationToken);

    public async Task<IReadOnlyList<FeaturePack>> ListAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = IncludeItems(_context.FeaturePacks);

        if (activeOnly.HasValue)
        {
            query = query.Where(fp => fp.IsActive == activeOnly.Value);
        }

        return await query.OrderBy(fp => fp.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FeaturePack featurePack, CancellationToken cancellationToken = default)
    {
        await _context.FeaturePacks.AddAsync(featurePack, cancellationToken);
    }

    private static IQueryable<FeaturePack> IncludeItems(IQueryable<FeaturePack> query)
        => query.Include(fp => fp.Items);
}
