using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Identity.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Identity.
/// TODO: Implementar regras de domínio, invariantes e domain events de Permission.
/// </summary>
public sealed class Permission : AuditableEntity<PermissionId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private Permission() { }
}

/// <summary>Identificador fortemente tipado de Permission.</summary>
public sealed record PermissionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PermissionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PermissionId From(Guid id) => new(id);
}
