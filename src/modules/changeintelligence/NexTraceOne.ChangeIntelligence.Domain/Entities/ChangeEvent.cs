using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.ChangeIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo ChangeIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de ChangeEvent.
/// </summary>
public sealed class ChangeEvent : AuditableEntity<ChangeEventId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ChangeEvent() { }
}

/// <summary>Identificador fortemente tipado de ChangeEvent.</summary>
public sealed record ChangeEventId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ChangeEventId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ChangeEventId From(Guid id) => new(id);
}
