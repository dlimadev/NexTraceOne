using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Audit.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Audit.
/// TODO: Implementar regras de domínio, invariantes e domain events de AuditEvent.
/// </summary>
public sealed class AuditEvent : AuditableEntity<AuditEventId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private AuditEvent() { }
}

/// <summary>Identificador fortemente tipado de AuditEvent.</summary>
public sealed record AuditEventId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AuditEventId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AuditEventId From(Guid id) => new(id);
}
