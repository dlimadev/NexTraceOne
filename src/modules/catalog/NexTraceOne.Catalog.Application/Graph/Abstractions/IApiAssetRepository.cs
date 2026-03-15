using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Repositório de ativos de API do módulo Catalog Graph.
/// </summary>
public interface IApiAssetRepository
{
    /// <summary>Obtém um ativo de API pelo identificador.</summary>
    Task<ApiAsset?> GetByIdAsync(ApiAssetId id, CancellationToken cancellationToken);

    /// <summary>Obtém um ativo de API pelo nome e serviço proprietário.</summary>
    Task<ApiAsset?> GetByNameAndOwnerAsync(string name, ServiceAssetId ownerServiceId, CancellationToken cancellationToken);

    /// <summary>Lista todos os ativos de API com seus grafos de consumidores.</summary>
    Task<IReadOnlyList<ApiAsset>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>Lista ativos de API pertencentes a um serviço específico.</summary>
    Task<IReadOnlyList<ApiAsset>> ListByServiceIdAsync(ServiceAssetId serviceId, CancellationToken cancellationToken);

    /// <summary>Pesquisa ativos de API pelo nome ou rota.</summary>
    Task<IReadOnlyList<ApiAsset>> SearchAsync(string searchTerm, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo ativo de API para persistência.</summary>
    void Add(ApiAsset apiAsset);
}
