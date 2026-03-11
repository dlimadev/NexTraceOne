using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.RulesetGovernance.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo RulesetGovernance.
/// TODO: Implementar regras de domínio, invariantes e domain events de LintFinding.
/// </summary>
public sealed class LintFinding : AuditableEntity<LintFindingId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private LintFinding() { }
}

/// <summary>Identificador fortemente tipado de LintFinding.</summary>
public sealed record LintFindingId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LintFindingId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LintFindingId From(Guid id) => new(id);
}
