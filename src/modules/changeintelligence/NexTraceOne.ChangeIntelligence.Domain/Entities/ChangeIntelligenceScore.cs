using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.ChangeIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo ChangeIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de ChangeIntelligenceScore.
/// </summary>
public sealed class ChangeIntelligenceScore : AuditableEntity<ChangeIntelligenceScoreId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ChangeIntelligenceScore() { }
}

/// <summary>Identificador fortemente tipado de ChangeIntelligenceScore.</summary>
public sealed record ChangeIntelligenceScoreId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ChangeIntelligenceScoreId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ChangeIntelligenceScoreId From(Guid id) => new(id);
}
