using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Contracts.Graph.DTOs;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Services;

/// <summary>
/// Implementação do contrato público do módulo Catalog Graph.
/// Outros módulos consomem esta interface — nunca o DbContext ou repositórios directamente.
/// </summary>
internal sealed class CatalogGraphModuleService(
    IApiAssetRepository apiAssetRepository,
    IServiceAssetRepository serviceAssetRepository,
    IContractVersionRepository contractVersionRepository) : ICatalogGraphModule
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamServiceInfo>> ListServicesByTeamAsync(string teamName, CancellationToken cancellationToken)
    {
        var services = await serviceAssetRepository.ListByTeamAsync(teamName, cancellationToken);

        return services
            .Select(svc => new TeamServiceInfo(
                svc.Id.Value.ToString(),
                svc.Name,
                svc.Domain,
                svc.Criticality.ToString(),
                svc.ExposureType.ToString()))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TeamContractInfo>> ListContractsByTeamAsync(string teamName, CancellationToken cancellationToken)
    {
        var services = await serviceAssetRepository.ListByTeamAsync(teamName, cancellationToken);
        if (services.Count == 0)
            return [];

        var allApiAssetIds = new List<Guid>();
        foreach (var service in services)
        {
            var apis = await apiAssetRepository.ListByServiceIdAsync(service.Id, cancellationToken);
            allApiAssetIds.AddRange(apis.Select(a => a.Id.Value));
        }

        if (allApiAssetIds.Count == 0)
            return [];

        var contractVersions = await contractVersionRepository.ListByApiAssetIdsAsync(
            allApiAssetIds, cancellationToken);

        return contractVersions
            .Select(cv => new TeamContractInfo(
                cv.Id.Value.ToString(),
                cv.SemVer,
                cv.Protocol.ToString(),
                cv.SemVer,
                cv.LifecycleState.ToString()))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CrossTeamDependencyInfo>> ListCrossTeamDependenciesAsync(string teamName, CancellationToken cancellationToken)
    {
        var services = await serviceAssetRepository.ListByTeamAsync(teamName, cancellationToken);
        if (services.Count == 0)
            return [];

        var dependencies = new List<CrossTeamDependencyInfo>();

        foreach (var service in services)
        {
            var apis = await apiAssetRepository.ListByServiceIdAsync(service.Id, cancellationToken);

            foreach (var api in apis)
            {
                foreach (var consumer in api.ConsumerRelationships)
                {
                    var consumerService = await serviceAssetRepository.GetByNameAsync(
                        consumer.ConsumerName, cancellationToken);

                    if (consumerService is null)
                        continue;

                    if (string.Equals(consumerService.TeamName, teamName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    dependencies.Add(new CrossTeamDependencyInfo(
                        consumer.Id.Value.ToString(),
                        consumerService.Name,
                        service.Name,
                        consumerService.TeamName,
                        consumerService.TeamName,
                        consumer.SourceType));
                }
            }
        }

        return dependencies;
    }
}
