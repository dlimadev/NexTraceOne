using NexTraceOne.EngineeringGraph.Domain.Entities;
using NexTraceOne.EngineeringGraph.Domain.Enums;

namespace NexTraceOne.EngineeringGraph.Application.Abstractions;

/// <summary>
/// Repositório de registros de saúde/métricas de nós do grafo.
/// Permite consulta de dados de overlay por nó, tipo e modo,
/// alimentando a camada de visualização com scores explicáveis.
/// </summary>
public interface INodeHealthRepository
{
    /// <summary>Obtém os registros de saúde mais recentes para um conjunto de nós em um overlay específico.</summary>
    Task<IReadOnlyList<NodeHealthRecord>> GetLatestByOverlayAsync(OverlayMode overlayMode, CancellationToken cancellationToken);

    /// <summary>Obtém o registro de saúde mais recente de um nó específico em um overlay.</summary>
    Task<NodeHealthRecord?> GetByNodeAsync(Guid nodeId, OverlayMode overlayMode, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo registro de saúde para persistência.</summary>
    void Add(NodeHealthRecord record);
}
