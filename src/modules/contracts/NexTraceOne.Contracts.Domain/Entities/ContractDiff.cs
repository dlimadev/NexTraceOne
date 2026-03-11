using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Contracts.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Contracts.
/// TODO: Implementar regras de domínio, invariantes e domain events de ContractDiff.
/// </summary>
public sealed class ContractDiff : AuditableEntity<ContractDiffId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ContractDiff() { }
}

/// <summary>Identificador fortemente tipado de ContractDiff.</summary>
public sealed record ContractDiffId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractDiffId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractDiffId From(Guid id) => new(id);
}
