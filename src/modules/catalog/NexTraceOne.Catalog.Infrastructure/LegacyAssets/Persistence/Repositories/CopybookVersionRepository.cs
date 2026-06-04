using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Infrastructure.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;

internal sealed class CopybookVersionRepository(ServiceCatalogDbContext context)
    : RepositoryBase<CopybookVersion, CopybookVersionId>(context), ICopybookVersionRepository
{
    private readonly ServiceCatalogDbContext _context = context;

    public override async Task<CopybookVersion?> GetByIdAsync(CopybookVersionId id, CancellationToken ct = default)
        => await _context.CopybookVersions.SingleOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<CopybookVersion>> ListByCopybookAsync(CopybookId copybookId, CancellationToken cancellationToken)
        => await _context.CopybookVersions
            .Where(v => v.CopybookId == copybookId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
}
