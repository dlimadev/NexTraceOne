using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Licensing.
/// TODO: Implementar regras de domínio, invariantes e domain events de LicenseActivation.
/// </summary>
public sealed class LicenseActivation : AuditableEntity<LicenseActivationId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private LicenseActivation() { }
}

/// <summary>Identificador fortemente tipado de LicenseActivation.</summary>
public sealed record LicenseActivationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LicenseActivationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LicenseActivationId From(Guid id) => new(id);
}
