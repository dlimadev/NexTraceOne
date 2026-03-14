using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.AiOrchestration.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo AiOrchestration.
/// TODO: Implementar regras de domínio, invariantes e domain events de AiConversation.
/// </summary>
public sealed class AiConversation : AuditableEntity<AiConversationId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private AiConversation() { }
}

/// <summary>Identificador fortemente tipado de AiConversation.</summary>
public sealed record AiConversationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiConversationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiConversationId From(Guid id) => new(id);
}
