using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Contracts.Contracts.ServiceInterfaces;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;
using Microsoft.EntityFrameworkCore;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Services;

/// <summary>
/// Implementação do contrato público do módulo Contracts.
/// Outros módulos consomem esta interface — nunca o DbContext ou repositórios directamente.
/// </summary>
internal sealed class ContractsModuleService(
    IContractVersionRepository contractVersionRepository,
    ContractsDbContext contractsDbContext,
    CatalogGraphDbContext graphDbContext) : IContractsModule
{
    /// <inheritdoc />
    public async Task<ChangeLevel?> GetLatestChangeLevelAsync(Guid apiAssetId, CancellationToken ct = default)
    {
        var latestVersion = await contractVersionRepository.GetLatestByApiAssetAsync(apiAssetId, ct);
        if (latestVersion is null)
        {
            return null;
        }

        var latestDiff = latestVersion.Diffs
            .OrderByDescending(d => d.ComputedAt)
            .FirstOrDefault();

        if (latestDiff is not null)
        {
            return latestDiff.ChangeLevel;
        }

        // Fallback para cenários onde o Aggregate foi carregado sem Include(Diffs).
        return await contractsDbContext.ContractDiffs
            .AsNoTracking()
            .Where(d => d.ContractVersionId == latestVersion.Id)
            .OrderByDescending(d => d.ComputedAt)
            .Select(d => (ChangeLevel?)d.ChangeLevel)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> HasContractVersionAsync(Guid apiAssetId, CancellationToken ct = default)
        => await contractVersionRepository.GetLatestByApiAssetAsync(apiAssetId, ct) is not null;

    /// <inheritdoc />
    public async Task<decimal?> GetLatestOverallScoreAsync(Guid apiAssetId, CancellationToken ct = default)
    {
        var latestVersion = await contractVersionRepository.GetLatestByApiAssetAsync(apiAssetId, ct);
        if (latestVersion is null)
        {
            return null;
        }

        return await contractsDbContext.ContractScorecards
            .AsNoTracking()
            .Where(s => s.ContractVersionId == latestVersion.Id)
            .OrderByDescending(s => s.ComputedAt)
            .Select(s => (decimal?)s.OverallScore)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> RequiresWorkflowApprovalAsync(Guid apiAssetId, CancellationToken ct = default)
    {
        var latestVersion = await contractVersionRepository.GetLatestByApiAssetAsync(apiAssetId, ct);
        if (latestVersion is null)
        {
            return false;
        }

        var latestChangeLevel = await GetLatestChangeLevelAsync(apiAssetId, ct);
        if (latestChangeLevel == ChangeLevel.Breaking)
        {
            return true;
        }

        return latestVersion.LifecycleState == ContractLifecycleState.InReview;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractBreakingChangeSummary>> GetRecentBreakingChangesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        int maxCount = 50,
        CancellationToken ct = default)
    {
        var diffs = await contractsDbContext.ContractDiffs
            .AsNoTracking()
            .Where(d => d.ChangeLevel == ChangeLevel.Breaking
                     && d.ComputedAt >= from
                     && d.ComputedAt <= to)
            .OrderByDescending(d => d.ComputedAt)
            .Take(maxCount)
            .ToListAsync(ct);

        if (diffs.Count == 0)
            return [];

        var assetIds = diffs.Select(d => d.ApiAssetId).Distinct().ToList();
        var assets = await graphDbContext.ApiAssets
            .AsNoTracking()
            .Include(a => a.OwnerService)
            .Where(a => assetIds.Contains(a.Id))
            .ToListAsync(ct);

        var assetMap = assets.ToDictionary(a => a.Id);

        return diffs
            .Select(d =>
            {
                assetMap.TryGetValue(d.ApiAssetId, out var asset);
                return new ContractBreakingChangeSummary(
                    ApiAssetId: d.ApiAssetId,
                    ApiAssetName: asset?.Name ?? d.ApiAssetId.ToString("N")[..8],
                    OwnerServiceName: asset?.OwnerService?.Name,
                    BreakingChangeCount: d.BreakingChanges.Count,
                    DetectedAt: d.ComputedAt);
            })
            .ToList()
            .AsReadOnly();
    }
}
