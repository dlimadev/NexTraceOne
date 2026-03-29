using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;

internal sealed class Db2ArtifactRepository(LegacyAssetsDbContext context)
    : RepositoryBase<Db2Artifact, Db2ArtifactId>(context), IDb2ArtifactRepository
{
    private readonly LegacyAssetsDbContext _context = context;

    public override async Task<Db2Artifact?> GetByIdAsync(Db2ArtifactId id, CancellationToken ct = default)
        => await _context.Db2Artifacts.SingleOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Db2Artifact?> GetByNameAndSystemAsync(string name, MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.Db2Artifacts
            .SingleOrDefaultAsync(a => a.Name == name && a.SystemId == systemId, cancellationToken);

    public async Task<IReadOnlyList<Db2Artifact>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.Db2Artifacts
            .Where(a => a.SystemId == systemId)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
}
