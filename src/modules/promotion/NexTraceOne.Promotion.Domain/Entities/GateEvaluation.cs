using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Promotion.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Promotion.
/// TODO: Implementar regras de domínio, invariantes e domain events de GateEvaluation.
/// </summary>
public sealed class GateEvaluation : AuditableEntity<GateEvaluationId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private GateEvaluation() { }
}

/// <summary>Identificador fortemente tipado de GateEvaluation.</summary>
public sealed record GateEvaluationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static GateEvaluationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static GateEvaluationId From(Guid id) => new(id);
}
