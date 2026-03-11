using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Contracts.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Contracts.
/// TODO: Implementar regras de domínio, invariantes e domain events de ContractLock.
/// </summary>
public sealed class ContractLock : AuditableEntity<ContractLockId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ContractLock() { }
}

/// <summary>Identificador fortemente tipado de ContractLock.</summary>
public sealed record ContractLockId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractLockId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractLockId From(Guid id) => new(id);
}
