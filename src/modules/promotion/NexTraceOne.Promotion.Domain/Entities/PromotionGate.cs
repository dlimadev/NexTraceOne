using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Promotion.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Promotion.
/// TODO: Implementar regras de domínio, invariantes e domain events de PromotionGate.
/// </summary>
public sealed class PromotionGate : AuditableEntity<PromotionGateId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private PromotionGate() { }
}

/// <summary>Identificador fortemente tipado de PromotionGate.</summary>
public sealed record PromotionGateId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PromotionGateId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PromotionGateId From(Guid id) => new(id);
}
