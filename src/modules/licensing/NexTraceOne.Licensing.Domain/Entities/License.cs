using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Licensing.
/// TODO: Implementar regras de domínio, invariantes e domain events de License.
/// </summary>
public sealed class License : AuditableEntity<LicenseId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private License() { }
}

/// <summary>Identificador fortemente tipado de License.</summary>
public sealed record LicenseId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LicenseId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LicenseId From(Guid id) => new(id);
}
