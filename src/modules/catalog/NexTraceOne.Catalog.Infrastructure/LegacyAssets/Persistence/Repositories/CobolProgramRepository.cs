using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;

internal sealed class CobolProgramRepository(LegacyAssetsDbContext context)
    : RepositoryBase<CobolProgram, CobolProgramId>(context), ICobolProgramRepository
{
    private readonly LegacyAssetsDbContext _context = context;

    public override async Task<CobolProgram?> GetByIdAsync(CobolProgramId id, CancellationToken ct = default)
        => await _context.CobolPrograms.SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<CobolProgram?> GetByNameAndSystemAsync(string name, MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.CobolPrograms
            .SingleOrDefaultAsync(p => p.Name == name && p.SystemId == systemId, cancellationToken);

    public async Task<IReadOnlyList<CobolProgram>> ListBySystemAsync(MainframeSystemId systemId, CancellationToken cancellationToken)
        => await _context.CobolPrograms
            .Where(p => p.SystemId == systemId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CobolProgram>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var term = searchTerm.Trim();
        if (term.Length == 0)
            return [];

        var pattern = $"%{term}%";
        return await _context.CobolPrograms
            .Where(p =>
                EF.Functions.Like(p.Name, pattern) ||
                EF.Functions.Like(p.DisplayName, pattern) ||
                EF.Functions.Like(p.Description, pattern))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
