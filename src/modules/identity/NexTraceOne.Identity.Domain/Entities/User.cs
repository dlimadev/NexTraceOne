using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Identity.
/// TODO: Implementar regras de domínio, invariantes e domain events de User.
/// </summary>
public sealed class User : AuditableEntity<UserId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private User() { }
}

/// <summary>Identificador fortemente tipado de User.</summary>
public sealed record UserId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static UserId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static UserId From(Guid id) => new(id);
}
