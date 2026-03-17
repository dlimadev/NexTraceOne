using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de decisões de roteamento de IA.
/// Suporta consulta por identificador e correlação.
/// </summary>
public interface IAiRoutingDecisionRepository
{
    /// <summary>Obtém decisão de roteamento por identificador.</summary>
    Task<AIRoutingDecision?> GetByIdAsync(AIRoutingDecisionId id, CancellationToken ct);

    /// <summary>Obtém decisão de roteamento por correlationId.</summary>
    Task<AIRoutingDecision?> GetByCorrelationIdAsync(string correlationId, CancellationToken ct);

    /// <summary>Adiciona nova decisão de roteamento.</summary>
    Task AddAsync(AIRoutingDecision decision, CancellationToken ct);

    /// <summary>Lista decisões recentes para visibilidade administrativa.</summary>
    Task<IReadOnlyList<AIRoutingDecision>> ListRecentAsync(int pageSize, CancellationToken ct);
}
