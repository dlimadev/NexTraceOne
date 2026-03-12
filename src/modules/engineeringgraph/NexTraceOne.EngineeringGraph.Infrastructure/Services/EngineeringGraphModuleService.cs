using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Contracts.ServiceInterfaces;
using NexTraceOne.EngineeringGraph.Domain.Entities;

namespace NexTraceOne.EngineeringGraph.Infrastructure.Services;

/// <summary>
/// Implementação do contrato público do módulo EngineeringGraph.
/// Outros módulos consomem esta interface — nunca o DbContext ou repositórios directamente.
/// </summary>
internal sealed class EngineeringGraphModuleService(
    IApiAssetRepository apiAssetRepository,
    IServiceAssetRepository serviceAssetRepository) : IEngineeringGraphModule
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
}
