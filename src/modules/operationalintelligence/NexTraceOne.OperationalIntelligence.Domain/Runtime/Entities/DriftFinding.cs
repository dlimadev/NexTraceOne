using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.RuntimeIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo RuntimeIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de DriftFinding.
/// </summary>
public sealed class DriftFinding : AuditableEntity<DriftFindingId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private DriftFinding() { }
}

/// <summary>Identificador fortemente tipado de DriftFinding.</summary>
public sealed record DriftFindingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static DriftFindingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static DriftFindingId From(Guid id) => new(id);
}
