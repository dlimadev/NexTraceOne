using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Repositório de links associados a serviços do catálogo.
/// Suporta listagem por serviço e operações CRUD.
/// </summary>
public interface IServiceLinkRepository
{
    /// <summary>Obtém um link pelo identificador.</summary>
    Task<ServiceLink?> GetByIdAsync(ServiceLinkId id, CancellationToken cancellationToken);

    /// <summary>Lista todos os links de um serviço, ordenados por categoria e sort order.</summary>
    Task<IReadOnlyList<ServiceLink>> ListByServiceAsync(ServiceAssetId serviceAssetId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo link para persistência.</summary>
    void Add(ServiceLink link);

    /// <summary>Remove um link existente.</summary>
    void Remove(ServiceLink link);
}
