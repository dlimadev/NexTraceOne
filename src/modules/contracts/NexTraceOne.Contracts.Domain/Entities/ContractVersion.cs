using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Contracts.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Contracts.
/// TODO: Implementar regras de domínio, invariantes e domain events de ContractVersion.
/// </summary>
public sealed class ContractVersion : AuditableEntity<ContractVersionId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ContractVersion() { }
}

/// <summary>Identificador fortemente tipado de ContractVersion.</summary>
public sealed record ContractVersionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractVersionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractVersionId From(Guid id) => new(id);
}
