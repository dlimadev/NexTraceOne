using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Identity.
/// TODO: Implementar regras de domínio, invariantes e domain events de Role.
/// </summary>
public sealed class Role : AuditableEntity<RoleId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private Role() { }
}

/// <summary>Identificador fortemente tipado de Role.</summary>
public sealed record RoleId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RoleId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RoleId From(Guid id) => new(id);
}
