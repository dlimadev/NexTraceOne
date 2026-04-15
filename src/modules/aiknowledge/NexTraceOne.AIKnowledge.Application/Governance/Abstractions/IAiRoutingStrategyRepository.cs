using NexTraceOne.AIKnowledge.Domain.Governance.Entities;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de estratégias de roteamento de IA.
/// Suporta listagem e consulta por contexto de execução.
/// </summary>
public interface IAiRoutingStrategyRepository
{
    /// <summary>Lista estratégias de roteamento com filtros opcionais.</summary>
    Task<IReadOnlyList<AIRoutingStrategy>> ListAsync(
        bool? isActive,
        CancellationToken ct);

    /// <summary>Obtém estratégia por identificador.</summary>
    Task<AIRoutingStrategy?> GetByIdAsync(AIRoutingStrategyId id, CancellationToken ct);

    /// <summary>Adiciona nova estratégia de roteamento.</summary>
    Task AddAsync(AIRoutingStrategy strategy, CancellationToken ct);

    /// <summary>Actualiza estratégia de roteamento existente.</summary>
    Task UpdateAsync(AIRoutingStrategy strategy, CancellationToken ct);
}
