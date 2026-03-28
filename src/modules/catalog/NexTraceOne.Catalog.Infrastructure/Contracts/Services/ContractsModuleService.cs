using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Contracts.Contracts.ServiceInterfaces;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Services;

/// <summary>
/// Implementação do contrato público do módulo Contracts.
/// Outros módulos consomem esta interface — nunca o DbContext ou repositórios directamente.
/// </summary>
internal sealed class ContractsModuleService(
    IContractVersionRepository contractVersionRepository,
    ContractsDbContext contractsDbContext) : IContractsModule
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
}
