using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Graph.Abstractions;

/// <summary>
/// Repositório de snapshots temporais do grafo de engenharia.
/// Permite persistência e consulta de estados materializados do grafo
/// para suporte a time-travel, diff e baseline.
/// </summary>
public interface IGraphSnapshotRepository
{
    /// <summary>Obtém um snapshot pelo identificador.</summary>
    Task<GraphSnapshot?> GetByIdAsync(GraphSnapshotId id, CancellationToken cancellationToken);

    /// <summary>Lista snapshots ordenados por data de captura (mais recente primeiro).</summary>
    Task<IReadOnlyList<GraphSnapshot>> ListAsync(int limit, CancellationToken cancellationToken);

    /// <summary>Obtém o snapshot mais recente disponível.</summary>
    Task<GraphSnapshot?> GetLatestAsync(CancellationToken cancellationToken);

    /// <summary>Adiciona um novo snapshot para persistência.</summary>
    void Add(GraphSnapshot snapshot);
}
