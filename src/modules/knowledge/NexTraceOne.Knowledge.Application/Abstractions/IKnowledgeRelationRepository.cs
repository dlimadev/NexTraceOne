using NexTraceOne.Knowledge.Domain.Entities;

namespace NexTraceOne.Knowledge.Application.Abstractions;

/// <summary>
/// Repositório de KnowledgeRelation.
/// </summary>
public interface IKnowledgeRelationRepository
{
    /// <summary>Obtém uma relação pelo identificador.</summary>
    Task<KnowledgeRelation?> GetByIdAsync(KnowledgeRelationId id, CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova relação.</summary>
    Task AddAsync(KnowledgeRelation relation, CancellationToken cancellationToken = default);

    /// <summary>Remove uma relação.</summary>
    void Remove(KnowledgeRelation relation);

    /// <summary>Lista relações por entidade de origem.</summary>
    Task<IReadOnlyList<KnowledgeRelation>> ListBySourceAsync(Guid sourceEntityId, CancellationToken cancellationToken = default);

    /// <summary>Lista relações por entidade de destino e tipo de relação.</summary>
    Task<IReadOnlyList<KnowledgeRelation>> ListByTargetAsync(
        NexTraceOne.Knowledge.Domain.Enums.RelationType targetType,
        Guid targetEntityId,
        CancellationToken cancellationToken = default);
}
