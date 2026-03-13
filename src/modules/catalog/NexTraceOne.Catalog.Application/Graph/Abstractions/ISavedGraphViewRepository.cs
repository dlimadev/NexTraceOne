using NexTraceOne.EngineeringGraph.Domain.Entities;

namespace NexTraceOne.EngineeringGraph.Application.Abstractions;

/// <summary>
/// Repositório de visões salvas do grafo de engenharia.
/// Permite persistência de preferências de visualização por usuário,
/// incluindo filtros, overlay, foco e layout para reprodução exata.
/// </summary>
public interface ISavedGraphViewRepository
{
    /// <summary>Obtém uma visão salva pelo identificador.</summary>
    Task<SavedGraphView?> GetByIdAsync(SavedGraphViewId id, CancellationToken cancellationToken);

    /// <summary>Lista visões salvas de um usuário (próprias + compartilhadas).</summary>
    Task<IReadOnlyList<SavedGraphView>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova visão salva para persistência.</summary>
    void Add(SavedGraphView view);
}
