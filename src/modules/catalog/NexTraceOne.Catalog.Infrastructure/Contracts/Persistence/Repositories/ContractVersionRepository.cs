using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Persistence.Repositories;

/// <summary>
/// Repositório de versões de contrato, implementando consultas específicas de negócio.
/// Suporta multi-protocolo e visão de catálogo de governança.
/// </summary>
internal sealed class ContractVersionRepository(ContractsDbContext context)
    : RepositoryBase<ContractVersion, ContractVersionId>(context), IContractVersionRepository
{
    /// <summary>Busca uma versão de contrato pelo Id, incluindo os diffs associados.</summary>
    public override async Task<ContractVersion?> GetByIdAsync(ContractVersionId id, CancellationToken ct = default)
        => await context.ContractVersions
            .Include(v => v.Diffs)
            .Include(v => v.RuleViolations)
            .Include(v => v.Artifacts)
            .SingleOrDefaultAsync(v => v.Id == id, ct);

    /// <summary>
    /// Busca uma versão de contrato pelo Id com todas as entidades relacionadas carregadas.
    /// Usa AsNoTracking para leitura de detalhe — adequado para consultas de exibição e análise.
    /// </summary>
    public async Task<ContractVersion?> GetDetailAsync(ContractVersionId id, CancellationToken ct = default)
        => await context.ContractVersions
            .AsNoTracking()
            .Include(v => v.Diffs)
            .Include(v => v.RuleViolations)
            .Include(v => v.Artifacts)
            .SingleOrDefaultAsync(v => v.Id == id, ct);

    /// <summary>Busca uma versão de contrato pelo ativo de API e versão semântica.</summary>
    public async Task<ContractVersion?> GetByApiAssetAndSemVerAsync(Guid apiAssetId, string semVer, CancellationToken ct = default)
        => await context.ContractVersions
            .Include(v => v.Diffs)
            .SingleOrDefaultAsync(v => v.ApiAssetId == apiAssetId && v.SemVer == semVer, ct);

    /// <summary>Lista todas as versões de contrato de um ativo de API, ordenadas por data de criação.</summary>
    public async Task<IReadOnlyList<ContractVersion>> ListByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.ContractVersions
            .AsNoTracking()
            .Where(v => v.ApiAssetId == apiAssetId)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Retorna a versão de contrato mais recente de um ativo de API.</summary>
    public async Task<ContractVersion?> GetLatestByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.ContractVersions
            .AsNoTracking()
            .Where(v => v.ApiAssetId == apiAssetId)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Pesquisa versões de contrato com filtros opcionais e paginação.
    /// Constrói a query de forma incremental conforme os filtros fornecidos.
    /// </summary>
    public async Task<(IReadOnlyList<ContractVersion> Items, int TotalCount)> SearchAsync(
        ContractProtocol? protocol,
        ContractLifecycleState? lifecycleState,
        Guid? apiAssetId,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.ContractVersions.AsNoTracking().AsQueryable();

        if (protocol.HasValue)
            query = query.Where(v => v.Protocol == protocol.Value);

        if (lifecycleState.HasValue)
            query = query.Where(v => v.LifecycleState == lifecycleState.Value);

        if (apiAssetId.HasValue)
            query = query.Where(v => v.ApiAssetId == apiAssetId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var tsQuery = EF.Functions.PlainToTsQuery("simple", term);

            var projected = query
                .Select(v => new
                {
                    Version = v,
                    SearchVector = EF.Functions.ToTsVector(
                        "simple",
                        (v.SemVer ?? string.Empty) + " " +
                        (v.ImportedFrom ?? string.Empty) + " " +
                        v.Protocol.ToString())
                })
                .Where(x => x.SearchVector.Matches(tsQuery));

            var totalCountFiltered = await projected.CountAsync(cancellationToken);
            var filteredItems = await projected
                .OrderByDescending(x => x.SearchVector.Rank(tsQuery))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Version)
                .ToListAsync(cancellationToken);

            return (filteredItems, totalCountFiltered);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Lista a versão mais recente de contrato por cada ApiAssetId distinto.
    /// Usado para a visão de catálogo de governança de contratos.
    /// </summary>
    public async Task<(IReadOnlyList<ContractVersion> Items, int TotalCount)> ListLatestPerApiAssetAsync(
        ContractProtocol? protocol,
        ContractLifecycleState? lifecycleState,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var latestQuery = context.ContractVersions
            .AsNoTracking()
            .GroupBy(v => v.ApiAssetId)
            .Select(g => g.OrderByDescending(v => v.CreatedAt).First());

        if (protocol.HasValue)
            latestQuery = latestQuery.Where(v => v.Protocol == protocol.Value);

        if (lifecycleState.HasValue)
            latestQuery = latestQuery.Where(v => v.LifecycleState == lifecycleState.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            latestQuery = latestQuery.Where(v =>
                EF.Functions.ILike(v.SemVer, $"%{term}%")
                || EF.Functions.ILike(v.ImportedFrom, $"%{term}%"));
        }

        var totalCount = await latestQuery.CountAsync(cancellationToken);
        var items = await latestQuery
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Lista versões de contrato para um conjunto de ApiAssetIds.
    /// Retorna apenas a versão mais recente por API asset.
    /// </summary>
    public async Task<IReadOnlyList<ContractVersion>> ListByApiAssetIdsAsync(
        IEnumerable<Guid> apiAssetIds,
        CancellationToken cancellationToken = default)
    {
        var ids = apiAssetIds.ToList();
        if (ids.Count == 0)
            return [];

        var versions = await context.ContractVersions
            .AsNoTracking()
            .Where(v => ids.Contains(v.ApiAssetId))
            .ToListAsync(cancellationToken);

        return versions
            .GroupBy(v => v.ApiAssetId)
            .Select(g => g.OrderByDescending(v => v.CreatedAt).First())
            .OrderByDescending(v => v.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Obtém contagens agregadas para o dashboard de governança de contratos.
    /// </summary>
    public async Task<ContractSummaryData> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalVersions = await context.ContractVersions.CountAsync(cancellationToken);
        var distinctContracts = await context.ContractVersions
            .Select(v => v.ApiAssetId)
            .Distinct()
            .CountAsync(cancellationToken);

        var byCombination = await context.ContractVersions
            .AsNoTracking()
            .GroupBy(v => new { v.Protocol, v.LifecycleState })
            .Select(g => new { g.Key.Protocol, g.Key.LifecycleState, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var draftCount = byCombination.Where(x => x.LifecycleState == ContractLifecycleState.Draft).Sum(x => x.Count);
        var inReviewCount = byCombination.Where(x => x.LifecycleState == ContractLifecycleState.InReview).Sum(x => x.Count);
        var approvedCount = byCombination.Where(x => x.LifecycleState == ContractLifecycleState.Approved).Sum(x => x.Count);
        var lockedCount = byCombination.Where(x => x.LifecycleState == ContractLifecycleState.Locked).Sum(x => x.Count);
        var deprecatedCount = byCombination.Where(x =>
            x.LifecycleState is ContractLifecycleState.Deprecated
                or ContractLifecycleState.Sunset
                or ContractLifecycleState.Retired).Sum(x => x.Count);

        var byProtocol = byCombination
            .GroupBy(x => x.Protocol.ToString())
            .Select(g => new ProtocolCount(g.Key, g.Sum(x => x.Count)))
            .OrderByDescending(p => p.Count)
            .ToList();

        return new ContractSummaryData(
            totalVersions,
            distinctContracts,
            draftCount,
            inReviewCount,
            approvedCount,
            lockedCount,
            deprecatedCount,
            byProtocol);
    }
}
