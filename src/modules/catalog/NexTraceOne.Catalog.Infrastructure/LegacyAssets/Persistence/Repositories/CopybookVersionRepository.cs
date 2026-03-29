using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;

internal sealed class CopybookVersionRepository(LegacyAssetsDbContext context)
    : RepositoryBase<CopybookVersion, CopybookVersionId>(context), ICopybookVersionRepository
{
    private readonly LegacyAssetsDbContext _context = context;

    public override async Task<CopybookVersion?> GetByIdAsync(CopybookVersionId id, CancellationToken ct = default)
        => await _context.CopybookVersions.SingleOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<CopybookVersion>> ListByCopybookAsync(CopybookId copybookId, CancellationToken cancellationToken)
        => await _context.CopybookVersions
            .Where(v => v.CopybookId == copybookId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
}
