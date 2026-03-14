using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.AiOrchestration.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo AiOrchestration.
/// TODO: Implementar regras de domínio, invariantes e domain events de KnowledgeCaptureEntry.
/// </summary>
public sealed class KnowledgeCaptureEntry : AuditableEntity<KnowledgeCaptureEntryId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private KnowledgeCaptureEntry() { }
}

/// <summary>Identificador fortemente tipado de KnowledgeCaptureEntry.</summary>
public sealed record KnowledgeCaptureEntryId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static KnowledgeCaptureEntryId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static KnowledgeCaptureEntryId From(Guid id) => new(id);
}
