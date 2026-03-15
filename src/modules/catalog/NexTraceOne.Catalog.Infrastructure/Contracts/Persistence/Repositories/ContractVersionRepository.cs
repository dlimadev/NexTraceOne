using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Infrastructure.Persistence.Repositories;

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
            .SingleOrDefaultAsync(v => v.Id == id, ct);

    /// <summary>Busca uma versão de contrato pelo ativo de API e versão semântica.</summary>
    public async Task<ContractVersion?> GetByApiAssetAndSemVerAsync(Guid apiAssetId, string semVer, CancellationToken ct = default)
        => await context.ContractVersions
            .Include(v => v.Diffs)
            .SingleOrDefaultAsync(v => v.ApiAssetId == apiAssetId && v.SemVer == semVer, ct);

    /// <summary>Lista todas as versões de contrato de um ativo de API, ordenadas por data de criação.</summary>
    public async Task<IReadOnlyList<ContractVersion>> ListByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.ContractVersions
            .Where(v => v.ApiAssetId == apiAssetId)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Retorna a versão de contrato mais recente de um ativo de API.</summary>
    public async Task<ContractVersion?> GetLatestByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default)
        => await context.ContractVersions
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
        var query = context.ContractVersions.AsQueryable();

        if (protocol.HasValue)
            query = query.Where(v => v.Protocol == protocol.Value);

        if (lifecycleState.HasValue)
            query = query.Where(v => v.LifecycleState == lifecycleState.Value);

        if (apiAssetId.HasValue)
            query = query.Where(v => v.ApiAssetId == apiAssetId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(v => EF.Functions.Like(v.SemVer, "%" + searchTerm + "%"));

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
        // Sub-query: obter o ID da versão mais recente por ApiAssetId
        var latestIdsQuery = context.ContractVersions
            .GroupBy(v => v.ApiAssetId)
            .Select(g => g.OrderByDescending(v => v.CreatedAt).First().Id);

        var query = context.ContractVersions
            .Where(v => latestIdsQuery.Contains(v.Id));

        if (protocol.HasValue)
            query = query.Where(v => v.Protocol == protocol.Value);

        if (lifecycleState.HasValue)
            query = query.Where(v => v.LifecycleState == lifecycleState.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm}%";
            query = query.Where(v =>
                EF.Functions.Like(v.SemVer, pattern) ||
                EF.Functions.Like(v.ImportedFrom, pattern));
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

        // Obter a versão mais recente por cada ApiAssetId no conjunto
        var latestIds = await context.ContractVersions
            .Where(v => ids.Contains(v.ApiAssetId))
            .GroupBy(v => v.ApiAssetId)
            .Select(g => g.OrderByDescending(v => v.CreatedAt).First().Id)
            .ToListAsync(cancellationToken);

        return await context.ContractVersions
            .Where(v => latestIds.Contains(v.Id))
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Obtém contagens agregadas para o dashboard de governança de contratos.
    /// </summary>
    public async Task<ContractSummaryData> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var allVersions = await context.ContractVersions
            .Select(v => new { v.ApiAssetId, v.Protocol, v.LifecycleState })
            .ToListAsync(cancellationToken);

        var totalVersions = allVersions.Count;
        var distinctContracts = allVersions.Select(v => v.ApiAssetId).Distinct().Count();
        var draftCount = allVersions.Count(v => v.LifecycleState == ContractLifecycleState.Draft);
        var inReviewCount = allVersions.Count(v => v.LifecycleState == ContractLifecycleState.InReview);
        var approvedCount = allVersions.Count(v => v.LifecycleState == ContractLifecycleState.Approved);
        var lockedCount = allVersions.Count(v => v.LifecycleState == ContractLifecycleState.Locked);
        var deprecatedCount = allVersions.Count(v =>
            v.LifecycleState is ContractLifecycleState.Deprecated or ContractLifecycleState.Sunset or ContractLifecycleState.Retired);

        var byProtocol = allVersions
            .GroupBy(v => v.Protocol.ToString())
            .Select(g => new ProtocolCount(g.Key, g.Count()))
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
