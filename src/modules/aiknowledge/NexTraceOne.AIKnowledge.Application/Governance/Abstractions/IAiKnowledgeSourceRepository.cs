using NexTraceOne.AiGovernance.Domain.Entities;
using NexTraceOne.AiGovernance.Domain.Enums;

namespace NexTraceOne.AiGovernance.Application.Abstractions;

/// <summary>
/// Repositório de fontes de conhecimento para grounding/contexto da IA.
/// Suporta listagem filtrada por tipo de fonte e estado de ativação.
/// </summary>
public interface IAiKnowledgeSourceRepository
{
    /// <summary>Lista fontes de conhecimento com filtros opcionais de tipo e estado ativo.</summary>
    Task<IReadOnlyList<AIKnowledgeSource>> ListAsync(
        KnowledgeSourceType? sourceType,
        bool? isActive,
        CancellationToken ct);
}
