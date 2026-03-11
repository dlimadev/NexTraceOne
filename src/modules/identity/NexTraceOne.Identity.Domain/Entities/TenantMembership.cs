using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Identity.
/// TODO: Implementar regras de domínio, invariantes e domain events de TenantMembership.
/// </summary>
public sealed class TenantMembership : AuditableEntity<TenantMembershipId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private TenantMembership() { }
}

/// <summary>Identificador fortemente tipado de TenantMembership.</summary>
public sealed record TenantMembershipId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static TenantMembershipId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static TenantMembershipId From(Guid id) => new(id);
}
