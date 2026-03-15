using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

internal sealed class ServiceAssetRepository(CatalogGraphDbContext context)
    : RepositoryBase<ServiceAsset, ServiceAssetId>(context), IServiceAssetRepository
{
    private readonly CatalogGraphDbContext _context = context;

    public override async Task<ServiceAsset?> GetByIdAsync(ServiceAssetId id, CancellationToken ct = default)
        => await _context.ServiceAssets.SingleOrDefaultAsync(svc => svc.Id == id, ct);

    public async Task<ServiceAsset?> GetByNameAsync(string name, CancellationToken cancellationToken)
        => await _context.ServiceAssets.SingleOrDefaultAsync(svc => svc.Name == name, cancellationToken);

    public async Task<IReadOnlyList<ServiceAsset>> ListAllAsync(CancellationToken cancellationToken)
        => await _context.ServiceAssets.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ServiceAsset>> ListFilteredAsync(
        string? teamName,
        string? domain,
        ServiceType? serviceType,
        Criticality? criticality,
        LifecycleStatus? lifecycleStatus,
        ExposureType? exposureType,
        string? searchTerm,
        CancellationToken cancellationToken)
    {
        var query = _context.ServiceAssets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(teamName))
            query = query.Where(s => s.TeamName == teamName);

        if (!string.IsNullOrWhiteSpace(domain))
            query = query.Where(s => s.Domain == domain);

        if (serviceType.HasValue)
            query = query.Where(s => s.ServiceType == serviceType.Value);

        if (criticality.HasValue)
            query = query.Where(s => s.Criticality == criticality.Value);

        if (lifecycleStatus.HasValue)
            query = query.Where(s => s.LifecycleStatus == lifecycleStatus.Value);

        if (exposureType.HasValue)
            query = query.Where(s => s.ExposureType == exposureType.Value);

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

    public async Task<IReadOnlyList<ServiceAsset>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var pattern = $"%{searchTerm}%";
        return await _context.ServiceAssets
            .Where(s =>
                EF.Functions.Like(s.Name, pattern) ||
                EF.Functions.Like(s.DisplayName, pattern) ||
                EF.Functions.Like(s.Domain, pattern) ||
                EF.Functions.Like(s.TeamName, pattern) ||
                EF.Functions.Like(s.Description, pattern))
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceAsset>> ListByTeamAsync(string teamName, CancellationToken cancellationToken)
        => await _context.ServiceAssets
            .Where(s => s.TeamName == teamName)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ServiceAsset>> ListByDomainAsync(string domain, CancellationToken cancellationToken)
        => await _context.ServiceAssets
            .Where(s => s.Domain == domain)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<int> CountByTeamAsync(string teamName, CancellationToken cancellationToken)
        => await _context.ServiceAssets
            .CountAsync(s => s.TeamName == teamName, cancellationToken);
}
