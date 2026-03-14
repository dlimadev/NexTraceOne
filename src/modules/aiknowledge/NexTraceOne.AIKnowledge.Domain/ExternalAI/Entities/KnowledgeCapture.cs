using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.ExternalAi.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo ExternalAi.
/// TODO: Implementar regras de domínio, invariantes e domain events de KnowledgeCapture.
/// </summary>
public sealed class KnowledgeCapture : AuditableEntity<KnowledgeCaptureId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private KnowledgeCapture() { }
}

/// <summary>Identificador fortemente tipado de KnowledgeCapture.</summary>
public sealed record KnowledgeCaptureId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static KnowledgeCaptureId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static KnowledgeCaptureId From(Guid id) => new(id);
}
