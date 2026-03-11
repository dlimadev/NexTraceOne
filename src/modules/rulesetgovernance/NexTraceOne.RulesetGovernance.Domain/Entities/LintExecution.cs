using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.RulesetGovernance.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo RulesetGovernance.
/// TODO: Implementar regras de domínio, invariantes e domain events de LintExecution.
/// </summary>
public sealed class LintExecution : AuditableEntity<LintExecutionId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private LintExecution() { }
}

/// <summary>Identificador fortemente tipado de LintExecution.</summary>
public sealed record LintExecutionId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LintExecutionId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LintExecutionId From(Guid id) => new(id);
}
