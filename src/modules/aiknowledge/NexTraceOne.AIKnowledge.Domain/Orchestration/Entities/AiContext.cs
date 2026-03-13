using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.AiOrchestration.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo AiOrchestration.
/// TODO: Implementar regras de domínio, invariantes e domain events de AiContext.
/// </summary>
public sealed class AiContext : AuditableEntity<AiContextId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private AiContext() { }
}

/// <summary>Identificador fortemente tipado de AiContext.</summary>
public sealed record AiContextId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AiContextId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AiContextId From(Guid id) => new(id);
}
