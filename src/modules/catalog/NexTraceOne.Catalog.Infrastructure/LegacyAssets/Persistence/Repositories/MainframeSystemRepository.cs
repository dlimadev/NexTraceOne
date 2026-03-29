using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

namespace NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence.Repositories;

internal sealed class MainframeSystemRepository(LegacyAssetsDbContext context)
    : RepositoryBase<MainframeSystem, MainframeSystemId>(context), IMainframeSystemRepository
{
    private readonly LegacyAssetsDbContext _context = context;

    public override async Task<MainframeSystem?> GetByIdAsync(MainframeSystemId id, CancellationToken ct = default)
        => await _context.MainframeSystems.SingleOrDefaultAsync(s => s.Id == id, ct);

    public async Task<MainframeSystem?> GetByNameAsync(string name, CancellationToken cancellationToken)
        => await _context.MainframeSystems.SingleOrDefaultAsync(s => s.Name == name, cancellationToken);

    public async Task<IReadOnlyList<MainframeSystem>> ListAllAsync(CancellationToken cancellationToken)
        => await _context.MainframeSystems.OrderBy(s => s.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MainframeSystem>> ListFilteredAsync(
        string? teamName, string? domain, Criticality? criticality,
        LifecycleStatus? lifecycleStatus, string? searchTerm,
        CancellationToken cancellationToken)
    {
        var query = _context.MainframeSystems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(teamName))
            query = query.Where(s => s.TeamName == teamName);

        if (!string.IsNullOrWhiteSpace(domain))
            query = query.Where(s => s.Domain == domain);

        if (criticality.HasValue)
            query = query.Where(s => s.Criticality == criticality.Value);

        if (lifecycleStatus.HasValue)
            query = query.Where(s => s.LifecycleStatus == lifecycleStatus.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm}%";
            query = query.Where(s =>
                EF.Functions.Like(s.Name, pattern) ||
                EF.Functions.Like(s.DisplayName, pattern) ||
                EF.Functions.Like(s.Domain, pattern) ||
                EF.Functions.Like(s.TeamName, pattern) ||
                EF.Functions.Like(s.Description, pattern));
        }

        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken)
        => await _context.MainframeSystems.CountAsync(cancellationToken);
}
