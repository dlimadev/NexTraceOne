using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Repositório de estados de deployment de ativos por ambiente.
/// Semântica de upsert: uma linha por (ServiceAssetId, Environment).
/// </summary>
public interface IAssetDeploymentStateRepository
{
    /// <summary>Obtém o estado de deployment de um serviço num ambiente específico.</summary>
    Task<AssetDeploymentState?> GetByServiceAndEnvironmentAsync(
        ServiceAssetId serviceAssetId,
        string environment,
        CancellationToken cancellationToken);

    /// <summary>Lista todos os ambientes onde um serviço está deployado.</summary>
    Task<IReadOnlyList<AssetDeploymentState>> ListByServiceAsync(
        ServiceAssetId serviceAssetId,
        CancellationToken cancellationToken);

    /// <summary>Lista todos os deployment states de um tenant (para dashboards).</summary>
    Task<IReadOnlyList<AssetDeploymentState>> ListByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    /// <summary>Adiciona novo estado de deployment.</summary>
    void Add(AssetDeploymentState state);
}
