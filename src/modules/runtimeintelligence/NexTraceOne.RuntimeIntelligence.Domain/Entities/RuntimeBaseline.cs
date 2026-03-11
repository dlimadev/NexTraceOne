using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.RuntimeIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo RuntimeIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de RuntimeBaseline.
/// </summary>
public sealed class RuntimeBaseline : AuditableEntity<RuntimeBaselineId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private RuntimeBaseline() { }
}

/// <summary>Identificador fortemente tipado de RuntimeBaseline.</summary>
public sealed record RuntimeBaselineId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RuntimeBaselineId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RuntimeBaselineId From(Guid id) => new(id);
}
