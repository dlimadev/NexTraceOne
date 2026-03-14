using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.RuntimeIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo RuntimeIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de RuntimeSnapshot.
/// </summary>
public sealed class RuntimeSnapshot : AuditableEntity<RuntimeSnapshotId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private RuntimeSnapshot() { }
}

/// <summary>Identificador fortemente tipado de RuntimeSnapshot.</summary>
public sealed record RuntimeSnapshotId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RuntimeSnapshotId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RuntimeSnapshotId From(Guid id) => new(id);
}
