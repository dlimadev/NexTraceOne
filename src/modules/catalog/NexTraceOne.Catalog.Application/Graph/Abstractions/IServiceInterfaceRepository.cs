using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Repositório de interfaces de serviço do módulo Catalog Graph.
/// Suporta listagem por serviço e gestão de ciclo de vida das interfaces.
/// </summary>
public interface IServiceInterfaceRepository
{
    /// <summary>Obtém uma interface de serviço pelo identificador.</summary>
    Task<ServiceInterface?> GetByIdAsync(ServiceInterfaceId id, CancellationToken ct);

    /// <summary>Lista todas as interfaces de um serviço específico.</summary>
    Task<IReadOnlyList<ServiceInterface>> ListByServiceAsync(Guid serviceAssetId, CancellationToken ct);

    /// <summary>Adiciona uma nova interface de serviço para persistência.</summary>
    void Add(ServiceInterface entity);
}
