using Microsoft.EntityFrameworkCore;

using NexTraceOne.AIKnowledge.Application.Runtime.Abstractions;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;

namespace NexTraceOne.AIKnowledge.Infrastructure.Context;

/// <summary>
/// Implementação do leitor de interfaces de serviço do Catálogo para grounding de IA.
/// Acesso somente-leitura ao CatalogGraphDbContext para ServiceInterface + ServiceAsset.
/// </summary>
public sealed class ServiceInterfaceGroundingReader(CatalogGraphDbContext catalogDb) : IServiceInterfaceGroundingReader
{
    public async Task<IReadOnlyList<ServiceInterfaceGroundingContext>> FindInterfacesByServiceAsync(
        string serviceIdentifier,
        int maxResults,
        CancellationToken ct = default)
    {
        var interfaces = await catalogDb.ServiceInterfaces
            .AsNoTracking()
            .Join(catalogDb.ServiceAssets,
                iface => iface.ServiceAssetId,
                asset => asset.Id.Value,
                (iface, asset) => new { iface, asset })
            .Where(x =>
                x.asset.Name == serviceIdentifier ||
                x.asset.DisplayName == serviceIdentifier ||
                x.asset.Id.Value.ToString() == serviceIdentifier)
            .OrderBy(x => x.iface.Name)
            .Take(maxResults)
            .Select(x => new
            {
                x.iface.Id,
                ServiceAssetId = x.iface.ServiceAssetId,
                ServiceName = x.asset.DisplayName,
                x.iface.Name,
                x.iface.Description,
                x.iface.InterfaceType,
                x.iface.Status,
                x.iface.ExposureScope,
                x.iface.SloTarget,
                x.iface.RequiresContract,
                x.iface.AuthScheme,
                x.iface.DeprecationDate,
            })
            .ToListAsync(ct);

        return interfaces.Select(x => new ServiceInterfaceGroundingContext(
            InterfaceId: x.Id.Value.ToString(),
            ServiceAssetId: x.ServiceAssetId.ToString(),
            ServiceName: x.ServiceName,
            Name: x.Name,
            Description: x.Description,
            InterfaceType: x.InterfaceType.ToString(),
            Status: x.Status.ToString(),
            ExposureScope: x.ExposureScope.ToString(),
            SloTarget: string.IsNullOrEmpty(x.SloTarget) ? null : x.SloTarget,
            RequiresContract: x.RequiresContract,
            AuthScheme: x.AuthScheme.ToString(),
            DeprecationDate: x.DeprecationDate)).ToList();
    }

    public async Task<IReadOnlyList<ServiceInterfaceGroundingContext>> FindInterfacesByNameAsync(
        string searchTerm,
        int maxResults,
        CancellationToken ct = default)
    {
        var term = searchTerm.Trim();
        if (term.Length == 0)
            return [];

        var interfaces = await catalogDb.ServiceInterfaces
            .AsNoTracking()
            .Join(catalogDb.ServiceAssets,
                iface => iface.ServiceAssetId,
                asset => asset.Id.Value,
                (iface, asset) => new { iface, asset })
            .Where(x => x.iface.Name.Contains(term) || x.iface.Description.Contains(term))
            .OrderBy(x => x.iface.Name)
            .Take(maxResults)
            .Select(x => new
            {
                x.iface.Id,
                ServiceAssetId = x.iface.ServiceAssetId,
                ServiceName = x.asset.DisplayName,
                x.iface.Name,
                x.iface.Description,
                x.iface.InterfaceType,
                x.iface.Status,
                x.iface.ExposureScope,
                x.iface.SloTarget,
                x.iface.RequiresContract,
                x.iface.AuthScheme,
                x.iface.DeprecationDate,
            })
            .ToListAsync(ct);

        return interfaces.Select(x => new ServiceInterfaceGroundingContext(
            InterfaceId: x.Id.Value.ToString(),
            ServiceAssetId: x.ServiceAssetId.ToString(),
            ServiceName: x.ServiceName,
            Name: x.Name,
            Description: x.Description,
            InterfaceType: x.InterfaceType.ToString(),
            Status: x.Status.ToString(),
            ExposureScope: x.ExposureScope.ToString(),
            SloTarget: string.IsNullOrEmpty(x.SloTarget) ? null : x.SloTarget,
            RequiresContract: x.RequiresContract,
            AuthScheme: x.AuthScheme.ToString(),
            DeprecationDate: x.DeprecationDate)).ToList();
    }
}
