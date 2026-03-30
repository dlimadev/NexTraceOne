using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Repositório de execuções do job de discovery automático.
/// Garante rastreabilidade e auditoria de cada execução.
/// </summary>
public interface IDiscoveryRunRepository
{
    /// <summary>Obtém uma execução pelo identificador.</summary>
    Task<DiscoveryRun?> GetByIdAsync(DiscoveryRunId id, CancellationToken cancellationToken);

    /// <summary>Lista as execuções mais recentes.</summary>
    Task<IReadOnlyList<DiscoveryRun>> ListRecentAsync(int top, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova execução.</summary>
    void Add(DiscoveryRun run);
}
