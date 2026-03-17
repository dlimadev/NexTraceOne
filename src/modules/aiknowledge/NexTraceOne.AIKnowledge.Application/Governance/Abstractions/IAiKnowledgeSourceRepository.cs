using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

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
