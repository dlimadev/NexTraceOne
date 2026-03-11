using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Licensing.
/// TODO: Implementar regras de domínio, invariantes e domain events de HardwareBinding.
/// </summary>
public sealed class HardwareBinding : AuditableEntity<HardwareBindingId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private HardwareBinding() { }
}

/// <summary>Identificador fortemente tipado de HardwareBinding.</summary>
public sealed record HardwareBindingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static HardwareBindingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static HardwareBindingId From(Guid id) => new(id);
}
