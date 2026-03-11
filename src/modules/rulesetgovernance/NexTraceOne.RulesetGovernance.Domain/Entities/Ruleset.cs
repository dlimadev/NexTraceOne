using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.RulesetGovernance.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo RulesetGovernance.
/// TODO: Implementar regras de domínio, invariantes e domain events de Ruleset.
/// </summary>
public sealed class Ruleset : AuditableEntity<RulesetId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private Ruleset() { }
}

/// <summary>Identificador fortemente tipado de Ruleset.</summary>
public sealed record RulesetId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RulesetId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RulesetId From(Guid id) => new(id);
}
