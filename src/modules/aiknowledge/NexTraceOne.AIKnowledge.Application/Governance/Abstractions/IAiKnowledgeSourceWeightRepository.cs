using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Repositório de pesos de fontes de conhecimento por caso de uso.
/// Suporta leitura de configurações persistidas e persistência de novas configurações.
/// </summary>
public interface IAiKnowledgeSourceWeightRepository
{
    /// <summary>
    /// Lista pesos de fontes de conhecimento, opcionalmente filtrados por caso de uso e estado.
    /// </summary>
    Task<IReadOnlyList<AIKnowledgeSourceWeight>> ListAsync(
        AIUseCaseType? useCaseType,
        bool? isActive,
        CancellationToken ct = default);

    /// <summary>Adiciona uma nova configuração de peso para persistência.</summary>
    Task AddAsync(AIKnowledgeSourceWeight weight, CancellationToken ct = default);

    /// <summary>Actualiza uma configuração de peso existente.</summary>
    Task UpdateAsync(AIKnowledgeSourceWeight weight, CancellationToken ct = default);
}
