using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.CostIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo CostIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de CostTrend.
/// </summary>
public sealed class CostTrend : AuditableEntity<CostTrendId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private CostTrend() { }
}

/// <summary>Identificador fortemente tipado de CostTrend.</summary>
public sealed record CostTrendId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CostTrendId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CostTrendId From(Guid id) => new(id);
}
