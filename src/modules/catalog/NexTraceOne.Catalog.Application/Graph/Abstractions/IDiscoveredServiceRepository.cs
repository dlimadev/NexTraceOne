using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Repositório de serviços descobertos automaticamente.
/// Suporta consulta filtrada por estado, ambiente e período.
/// </summary>
public interface IDiscoveredServiceRepository
{
    /// <summary>Obtém um serviço descoberto pelo identificador.</summary>
    Task<DiscoveredService?> GetByIdAsync(DiscoveredServiceId id, CancellationToken cancellationToken);

    /// <summary>Obtém um serviço descoberto pelo nome e ambiente (unicidade lógica).</summary>
    Task<DiscoveredService?> GetByNameAndEnvironmentAsync(string serviceName, string environment, CancellationToken cancellationToken);

    /// <summary>Lista serviços descobertos com filtros opcionais.</summary>
    Task<IReadOnlyList<DiscoveredService>> ListFilteredAsync(
        DiscoveryStatus? status,
        string? environment,
        string? searchTerm,
        CancellationToken cancellationToken);

    /// <summary>Conta serviços descobertos por estado.</summary>
    Task<int> CountByStatusAsync(DiscoveryStatus status, CancellationToken cancellationToken);

    /// <summary>Conta serviços novos (Pending) descobertos numa janela temporal.</summary>
    Task<int> CountNewSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>Adiciona novo serviço descoberto.</summary>
    void Add(DiscoveredService discoveredService);
}
