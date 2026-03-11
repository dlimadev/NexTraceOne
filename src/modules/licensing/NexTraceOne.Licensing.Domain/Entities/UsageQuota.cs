using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Licensing.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Licensing.
/// TODO: Implementar regras de domínio, invariantes e domain events de UsageQuota.
/// </summary>
public sealed class UsageQuota : AuditableEntity<UsageQuotaId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private UsageQuota() { }
}

/// <summary>Identificador fortemente tipado de UsageQuota.</summary>
public sealed record UsageQuotaId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static UsageQuotaId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static UsageQuotaId From(Guid id) => new(id);
}
