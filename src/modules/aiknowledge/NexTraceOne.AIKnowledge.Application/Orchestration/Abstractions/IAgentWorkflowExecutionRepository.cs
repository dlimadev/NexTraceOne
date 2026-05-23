using NexTraceOne.AIKnowledge.Domain.Orchestration.Entities;

namespace NexTraceOne.AIKnowledge.Application.Orchestration.Abstractions;

/// <summary>
/// Repositório de execuções de workflows multi-agent.
/// Suporta auditoria, listagem paginada e consulta por correlação.
/// </summary>
public interface IAgentWorkflowExecutionRepository
{
    /// <summary>Obtém uma execução pelo identificador.</summary>
    Task<AgentWorkflowExecution?> GetByIdAsync(AgentWorkflowExecutionId id, CancellationToken ct);

    /// <summary>Lista execuções de um workflow específico.</summary>
    Task<IReadOnlyList<AgentWorkflowExecution>> ListByWorkflowAsync(
        string workflowName, int page, int pageSize, CancellationToken ct);

    /// <summary>Lista execuções recentes com paginação.</summary>
    Task<IReadOnlyList<AgentWorkflowExecution>> ListRecentAsync(
        int page, int pageSize, CancellationToken ct);

    /// <summary>Lista execuções por team caller.</summary>
    Task<IReadOnlyList<AgentWorkflowExecution>> ListByCallerTeamAsync(
        string callerTeamId, int page, int pageSize, CancellationToken ct);

    /// <summary>Adiciona uma nova execução.</summary>
    Task AddAsync(AgentWorkflowExecution execution, CancellationToken ct);

    /// <summary>Atualiza uma execução existente.</summary>
    Task UpdateAsync(AgentWorkflowExecution execution, CancellationToken ct);
}
