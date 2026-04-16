using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Context;

/// <summary>
/// Implementação do leitor de versões de contrato para grounding de IA.
/// Acesso somente-leitura ao ContractsDbContext do módulo Catalog.
/// Também acede ao CatalogGraphDbContext para navegar ServiceInterface → ContractBinding → ContractVersion.
/// </summary>
public sealed class ContractGroundingReader(
    ContractsDbContext contractsDb,
    CatalogGraphDbContext catalogDb) : IContractGroundingReader
{
    public async Task<IReadOnlyList<ContractGroundingContext>> FindContractVersionsAsync(
        Guid? contractVersionId,
        Guid? apiAssetId,
        string? searchTerm,
        int maxResults,
        CancellationToken ct = default)
    {
        var query = contractsDb.ContractVersions.AsNoTracking();

        if (contractVersionId.HasValue)
            query = query.Where(cv => cv.Id == ContractVersionId.From(contractVersionId.Value));

        if (apiAssetId.HasValue)
            query = query.Where(cv => cv.ApiAssetId == apiAssetId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(cv => cv.SemVer.Contains(searchTerm));

        var versions = await query
            .OrderByDescending(cv => cv.CreatedAt)
            .Take(maxResults)
            .ToListAsync(ct);

        return versions.Select(cv => new ContractGroundingContext(
            ContractVersionId: cv.Id.Value.ToString(),
            ApiAssetId: cv.ApiAssetId.ToString(),
            Version: cv.SemVer,
            Protocol: cv.Protocol.ToString(),
            LifecycleState: cv.LifecycleState.ToString(),
            IsLocked: cv.IsLocked,
            LockedAt: cv.LockedAt)).ToList();
    }

    public async Task<IReadOnlyList<ContractGroundingContext>> FindContractsByServiceInterfaceAsync(
        Guid serviceInterfaceId,
        string? environment,
        int maxResults,
        CancellationToken ct = default)
    {
        var bindingQuery = catalogDb.ContractBindings
            .AsNoTracking()
            .Where(cb => cb.ServiceInterfaceId == serviceInterfaceId
                      && cb.Status == ContractBindingStatus.Active);

        if (!string.IsNullOrWhiteSpace(environment))
            bindingQuery = bindingQuery.Where(cb => cb.BindingEnvironment == environment);

        var contractVersionIds = await bindingQuery
            .Select(cb => cb.ContractVersionId)
            .ToListAsync(ct);

        if (contractVersionIds.Count == 0)
            return [];

        var versions = await contractsDb.ContractVersions
            .AsNoTracking()
            .Where(cv => contractVersionIds.Contains(cv.Id.Value))
            .OrderByDescending(cv => cv.CreatedAt)
            .Take(maxResults)
            .ToListAsync(ct);

        return versions.Select(cv => new ContractGroundingContext(
            ContractVersionId: cv.Id.Value.ToString(),
            ApiAssetId: cv.ApiAssetId.ToString(),
            Version: cv.SemVer,
            Protocol: cv.Protocol.ToString(),
            LifecycleState: cv.LifecycleState.ToString(),
            IsLocked: cv.IsLocked,
            LockedAt: cv.LockedAt)).ToList();
    }
}
