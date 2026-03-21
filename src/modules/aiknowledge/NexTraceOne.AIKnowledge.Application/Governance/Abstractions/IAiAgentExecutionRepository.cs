using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de execuções de agents de IA.
/// Suporta listagem por agent e utilizador, e obtenção individual.
/// </summary>
public interface IAiAgentExecutionRepository
{
    /// <summary>Obtém uma execução pelo identificador.</summary>
    Task<AiAgentExecution?> GetByIdAsync(AiAgentExecutionId id, CancellationToken ct);

    /// <summary>Lista execuções de um agent específico.</summary>
    Task<IReadOnlyList<AiAgentExecution>> ListByAgentAsync(
        AiAgentId agentId, int pageSize, CancellationToken ct);

    /// <summary>Lista execuções de um utilizador.</summary>
    Task<IReadOnlyList<AiAgentExecution>> ListByUserAsync(
        string userId, int pageSize, CancellationToken ct);

    /// <summary>Adiciona uma nova execução.</summary>
    Task AddAsync(AiAgentExecution execution, CancellationToken ct);

    /// <summary>Atualiza uma execução existente.</summary>
    Task UpdateAsync(AiAgentExecution execution, CancellationToken ct);
}
