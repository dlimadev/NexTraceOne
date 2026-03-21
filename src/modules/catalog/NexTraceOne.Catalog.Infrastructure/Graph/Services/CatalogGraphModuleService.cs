using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Services;

/// <summary>
/// Implementação do contrato público do módulo Catalog Graph.
/// Outros módulos consomem esta interface — nunca o DbContext ou repositórios directamente.
/// </summary>
internal sealed class CatalogGraphModuleService(
    IApiAssetRepository apiAssetRepository,
    IServiceAssetRepository serviceAssetRepository) : ICatalogGraphModule
{
    /// <inheritdoc />
    public async Task<bool> ApiAssetExistsAsync(Guid apiAssetId, CancellationToken cancellationToken)
    {
        var asset = await apiAssetRepository.GetByIdAsync(ApiAssetId.From(apiAssetId), cancellationToken);
        return asset is not null;
    }

    /// <inheritdoc />
    public async Task<bool> ServiceAssetExistsAsync(string serviceName, CancellationToken cancellationToken)
    {
        var service = await serviceAssetRepository.GetByNameAsync(serviceName, cancellationToken);
        return service is not null;
    }

    /// <inheritdoc />
    public async Task<int> CountServicesByTeamAsync(string teamName, CancellationToken cancellationToken)
        => await serviceAssetRepository.CountByTeamAsync(teamName, cancellationToken);

    /// <inheritdoc />
    public async Task<int> CountServicesByDomainAsync(string domain, CancellationToken cancellationToken)
        => await serviceAssetRepository.CountByDomainAsync(domain, cancellationToken);
}
