using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Promotion.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Promotion.
/// TODO: Implementar regras de domínio, invariantes e domain events de PromotionRequest.
/// </summary>
public sealed class PromotionRequest : AuditableEntity<PromotionRequestId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private PromotionRequest() { }
}

/// <summary>Identificador fortemente tipado de PromotionRequest.</summary>
public sealed record PromotionRequestId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static PromotionRequestId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static PromotionRequestId From(Guid id) => new(id);
}
