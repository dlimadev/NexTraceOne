using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.Contracts.Domain.Entities;

/// <summary>
/// Value object que representa o estado de bloqueio de uma versão de contrato.
/// Utilizado internamente por ContractVersion para encapsular os dados de lock.
/// </summary>
public sealed record ContractLock(string LockedBy, DateTimeOffset LockedAt);

/// <summary>Identificador fortemente tipado de ContractLock (mantido para compatibilidade).</summary>
public sealed record ContractLockId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ContractLockId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ContractLockId From(Guid id) => new(id);
}
