using NexTraceOne.EngineeringGraph.Domain.Entities;

namespace NexTraceOne.EngineeringGraph.Application.Abstractions;

/// <summary>
/// Repositório de ativos de serviço do módulo EngineeringGraph.
/// </summary>
public interface IServiceAssetRepository
{
    /// <summary>Obtém um ativo de serviço pelo identificador.</summary>
    Task<ServiceAsset?> GetByIdAsync(ServiceAssetId id, CancellationToken cancellationToken);

    /// <summary>Obtém um ativo de serviço pelo nome único.</summary>
    Task<ServiceAsset?> GetByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>Lista todos os ativos de serviço registrados.</summary>
    Task<IReadOnlyList<ServiceAsset>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>Adiciona um novo ativo de serviço para persistência.</summary>
    void Add(ServiceAsset serviceAsset);
}
