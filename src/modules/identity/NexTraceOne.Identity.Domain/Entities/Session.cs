using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Identity.
/// TODO: Implementar regras de domínio, invariantes e domain events de Session.
/// </summary>
public sealed class Session : AuditableEntity<SessionId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private Session() { }
}

/// <summary>Identificador fortemente tipado de Session.</summary>
public sealed record SessionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static SessionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static SessionId From(Guid id) => new(id);
}
