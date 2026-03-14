using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.CostIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo CostIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de CostSnapshot.
/// </summary>
public sealed class CostSnapshot : AuditableEntity<CostSnapshotId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private CostSnapshot() { }
}

/// <summary>Identificador fortemente tipado de CostSnapshot.</summary>
public sealed record CostSnapshotId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CostSnapshotId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CostSnapshotId From(Guid id) => new(id);
}
