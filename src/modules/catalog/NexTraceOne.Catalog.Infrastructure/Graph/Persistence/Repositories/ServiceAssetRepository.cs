using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

internal sealed class ServiceAssetRepository(CatalogGraphDbContext context, ICurrentTenant currentTenant)
    : RepositoryBase<ServiceAsset, ServiceAssetId>(context), IServiceAssetRepository
{
    private readonly CatalogGraphDbContext _context = context;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    public override async Task<ServiceAsset?> GetByIdAsync(ServiceAssetId id, CancellationToken ct = default)
        => await _context.ServiceAssets
            .Where(svc => svc.TenantId == _currentTenant.Id)
            .SingleOrDefaultAsync(svc => svc.Id == id, ct);

    /// <summary>
    /// Busca um ativo de serviço pelo Id com leitura somente (AsNoTracking).
    /// Adequado para consultas de detalhe de serviço na UI ou relatórios.
    /// ServiceAsset não possui coleções de navegação no modelo actual — retorna o agregado completo.
    /// </summary>
    public async Task<ServiceAsset?> GetDetailAsync(ServiceAssetId id, CancellationToken ct = default)
        => await _context.ServiceAssets
            .AsNoTracking()
            .Where(svc => svc.TenantId == _currentTenant.Id)
            .SingleOrDefaultAsync(svc => svc.Id == id, ct);

    public async Task<ServiceAsset?> GetByNameAsync(string name, CancellationToken cancellationToken)
        => await _context.ServiceAssets
            .Where(svc => svc.TenantId == _currentTenant.Id)
            .SingleOrDefaultAsync(svc => svc.Name == name, cancellationToken);

    public async Task<IReadOnlyList<ServiceAsset>> ListAllAsync(CancellationToken cancellationToken)
        => await _context.ServiceAssets
            .AsNoTracking()
            .Where(svc => svc.TenantId == _currentTenant.Id)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<ServiceAsset> Items, int TotalCount)> ListFilteredAsync(
        string? teamName,
        string? domain,
        ServiceType? serviceType,
        Criticality? criticality,
        LifecycleStatus? lifecycleStatus,
        ExposureType? exposureType,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.ServiceAssets
            .AsNoTracking()
            .Where(svc => svc.TenantId == _currentTenant.Id)
            .AsQueryable();

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

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<ServiceAsset>> SearchAsync(string searchTerm, CancellationToken cancellationToken)
    {
        var term = searchTerm.Trim();
        if (term.Length == 0)
            return [];

        var tsQuery = EF.Functions.PlainToTsQuery("simple", term);

        return await _context.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == _currentTenant.Id)
            .Where(s => s.SearchVector.Matches(tsQuery))
            .OrderByDescending(s => s.SearchVector.Rank(tsQuery))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceAsset>> ListByTeamAsync(string teamName, CancellationToken cancellationToken)
        => await _context.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == _currentTenant.Id)
            .Where(s => s.TeamName == teamName)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ServiceAsset>> ListByDomainAsync(string domain, CancellationToken cancellationToken)
        => await _context.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == _currentTenant.Id)
            .Where(s => s.Domain == domain)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ServiceAsset>> ListBySubDomainAsync(string subDomain, CancellationToken cancellationToken)
        => await _context.ServiceAssets
            .AsNoTracking()
            .Where(s => s.TenantId == _currentTenant.Id)
            .Where(s => s.SubDomain == subDomain)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task<int> CountByTeamAsync(string teamName, CancellationToken cancellationToken)
        => await _context.ServiceAssets
            .Where(s => s.TenantId == _currentTenant.Id)
            .CountAsync(s => s.TeamName == teamName, cancellationToken);

    public async Task<int> CountByDomainAsync(string domain, CancellationToken cancellationToken)
        => await _context.ServiceAssets
            .Where(s => s.TenantId == _currentTenant.Id)
            .CountAsync(s => s.Domain == domain, cancellationToken);
}
