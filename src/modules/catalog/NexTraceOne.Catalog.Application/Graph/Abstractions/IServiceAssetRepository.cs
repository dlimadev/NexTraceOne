using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Repositório de ativos de serviço do módulo Catalog Graph.
/// Suporta listagem filtrada para o catálogo de serviços e gestão de ownership.
/// </summary>
public interface IServiceAssetRepository
{
    /// <summary>Obtém um ativo de serviço pelo identificador.</summary>
    Task<ServiceAsset?> GetByIdAsync(ServiceAssetId id, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém um ativo de serviço pelo identificador para leitura de detalhe (AsNoTracking).
    /// Recomendado para consultas de exibição de detalhe de serviço.
    /// </summary>
    Task<ServiceAsset?> GetDetailAsync(ServiceAssetId id, CancellationToken cancellationToken);

    /// <summary>Obtém um ativo de serviço pelo nome único.</summary>
    Task<ServiceAsset?> GetByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>Lista todos os ativos de serviço registrados.</summary>
    Task<IReadOnlyList<ServiceAsset>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>Lista serviços com filtros opcionais para o catálogo.</summary>
    Task<IReadOnlyList<ServiceAsset>> ListFilteredAsync(
        string? teamName,
        string? domain,
        ServiceType? serviceType,
        Criticality? criticality,
        LifecycleStatus? lifecycleStatus,
        ExposureType? exposureType,
        string? searchTerm,
        CancellationToken cancellationToken);

    /// <summary>Pesquisa serviços por termo textual (nome, domínio, equipa, descrição).</summary>
    Task<IReadOnlyList<ServiceAsset>> SearchAsync(string searchTerm, CancellationToken cancellationToken);

    /// <summary>Lista serviços de uma equipa específica.</summary>
    Task<IReadOnlyList<ServiceAsset>> ListByTeamAsync(string teamName, CancellationToken cancellationToken);

    /// <summary>Lista serviços de um domínio específico.</summary>
    Task<IReadOnlyList<ServiceAsset>> ListByDomainAsync(string domain, CancellationToken cancellationToken);

    /// <summary>Conta o total de serviços por equipa.</summary>
    Task<int> CountByTeamAsync(string teamName, CancellationToken cancellationToken);

    /// <summary>Conta o total de serviços por domínio.</summary>
    Task<int> CountByDomainAsync(string domain, CancellationToken cancellationToken);

    /// <summary>Lista serviços de um subdomínio específico.</summary>
    Task<IReadOnlyList<ServiceAsset>> ListBySubDomainAsync(string subDomain, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo ativo de serviço para persistência.</summary>
    void Add(ServiceAsset serviceAsset);
}
