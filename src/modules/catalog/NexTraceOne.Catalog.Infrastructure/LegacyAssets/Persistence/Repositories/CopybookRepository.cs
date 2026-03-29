using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;

internal sealed class CopybookRepository(LegacyAssetsDbContext context)
    : RepositoryBase<Copybook, CopybookId>(context), ICopybookRepository
{
    private readonly LegacyAssetsDbContext _context = context;

    public override async Task<Copybook?> GetByIdAsync(CopybookId id, CancellationToken ct = default)
        => await _context.Copybooks.SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Copybook?> GetByNameAndSystemAsync(string name, MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.Copybooks
            .SingleOrDefaultAsync(c => c.Name == name && c.SystemId == systemId, cancellationToken);

    public async Task<IReadOnlyList<Copybook>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.Copybooks
            .Where(c => c.SystemId == systemId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Copybook>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var term = searchTerm.Trim();
        if (term.Length == 0)
            return [];

        var pattern = $"%{term}%";
        return await _context.Copybooks
            .Where(c =>
                EF.Functions.Like(c.Name, pattern) ||
                EF.Functions.Like(c.DisplayName, pattern) ||
                EF.Functions.Like(c.Description, pattern))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
