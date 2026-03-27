using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para KnowledgeRelation.
/// </summary>
public sealed record KnowledgeRelationId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Entidade que representa uma relação entre um objecto de conhecimento
/// e outra entidade do sistema NexTraceOne (serviço, contrato, mudança, incidente, etc.).
///
/// Permite criar um grafo de conhecimento contextualizado, ligando documentos
/// e notas operacionais aos recursos relevantes sem acoplamento directo entre módulos.
///
/// Owner: módulo Knowledge.
/// Pilar: Source of Truth &amp; Operational Knowledge.
/// </summary>
public sealed class KnowledgeRelation : Entity<KnowledgeRelationId>
{
    /// <summary>
    /// Identificador do objecto de conhecimento de origem (KnowledgeDocument ou OperationalNote).
    /// </summary>
    public Guid SourceEntityId { get; private init; }

    /// <summary>Tipo do objecto de origem (KnowledgeDocument ou OperationalNote).</summary>
    public KnowledgeSourceEntityType SourceEntityType { get; private init; }

    /// <summary>
    /// Identificador da entidade de destino (ServiceId, ContractId, ReleaseId, etc.).
    /// </summary>
    public Guid TargetEntityId { get; private init; }

    /// <summary>
    /// Tipo da entidade de destino conforme o enum RelationType.
    /// </summary>
    public RelationType TargetType { get; private init; }

    /// <summary>Descrição opcional da relação.</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Contexto opcional da relação (ex: "Runbook", "PostMortem", "Mitigation").
    /// Facilita navegação e filtros sem acoplar o domínio Knowledge aos outros módulos.
    /// </summary>
    public string? Context { get; private set; }

    /// <summary>Identificador de quem criou a relação (UserId).</summary>
    public Guid CreatedById { get; private init; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    private KnowledgeRelation() { }

    /// <summary>Cria uma nova relação de conhecimento.</summary>
    public static KnowledgeRelation Create(
        Guid sourceEntityId,
        KnowledgeSourceEntityType sourceEntityType,
        Guid targetEntityId,
        RelationType targetType,
        string? description,
        string? context,
        Guid createdById,
        DateTimeOffset utcNow)
    {
        Guard.Against.Default(sourceEntityId, nameof(sourceEntityId));
        Guard.Against.Default(targetEntityId, nameof(targetEntityId));
        if (context is not null)
        {
            Guard.Against.StringTooLong(context, 100, nameof(context));
        }
        Guard.Against.Default(createdById, nameof(createdById));

        return new KnowledgeRelation
        {
            Id = new KnowledgeRelationId(Guid.NewGuid()),
            SourceEntityId = sourceEntityId,
            SourceEntityType = sourceEntityType,
            TargetEntityId = targetEntityId,
            TargetType = targetType,
            Description = description?.Trim(),
            Context = context?.Trim(),
            CreatedById = createdById,
            CreatedAt = utcNow
        };
    }

    /// <summary>Atualiza a descrição da relação.</summary>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
    }

    /// <summary>Atualiza o contexto textual da relação.</summary>
    public void UpdateContext(string? context)
    {
        if (context is not null)
        {
            Guard.Against.StringTooLong(context, 100, nameof(context));
        }
        Context = context?.Trim();
    }
}
