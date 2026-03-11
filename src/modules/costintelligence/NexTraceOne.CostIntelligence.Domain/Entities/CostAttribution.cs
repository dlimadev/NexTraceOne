using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.CostIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo CostIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de CostAttribution.
/// </summary>
public sealed class CostAttribution : AuditableEntity<CostAttributionId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private CostAttribution() { }
}

/// <summary>Identificador fortemente tipado de CostAttribution.</summary>
public sealed record CostAttributionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static CostAttributionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static CostAttributionId From(Guid id) => new(id);
}
