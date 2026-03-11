using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Licensing.
/// TODO: Implementar regras de domínio, invariantes e domain events de LicenseCapability.
/// </summary>
public sealed class LicenseCapability : AuditableEntity<LicenseCapabilityId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private LicenseCapability() { }
}

/// <summary>Identificador fortemente tipado de LicenseCapability.</summary>
public sealed record LicenseCapabilityId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LicenseCapabilityId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LicenseCapabilityId From(Guid id) => new(id);
}
