using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.ChangeIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo ChangeIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de DeploymentState.
/// </summary>
public sealed class DeploymentState : AuditableEntity<DeploymentStateId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private DeploymentState() { }
}

/// <summary>Identificador fortemente tipado de DeploymentState.</summary>
public sealed record DeploymentStateId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static DeploymentStateId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static DeploymentStateId From(Guid id) => new(id);
}
