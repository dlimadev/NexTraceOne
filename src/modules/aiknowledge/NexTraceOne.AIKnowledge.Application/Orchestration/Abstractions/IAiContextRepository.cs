using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;

/// <summary>
/// Repositório de contextos montados para consultas de IA no módulo de orquestração.
/// </summary>
public interface IAiContextRepository
{
    /// <summary>Obtém um contexto pelo identificador.</summary>
    Task<AiContext?> GetByIdAsync(AiContextId id, CancellationToken ct);

    /// <summary>Adiciona e persiste um novo contexto.</summary>
    Task AddAsync(AiContext context, CancellationToken ct);

    /// <summary>Lista os contextos mais recentes de um serviço.</summary>
    Task<IReadOnlyList<AiContext>> GetRecentByServiceAsync(
        string serviceName,
        int maxCount,
        CancellationToken ct);
}
