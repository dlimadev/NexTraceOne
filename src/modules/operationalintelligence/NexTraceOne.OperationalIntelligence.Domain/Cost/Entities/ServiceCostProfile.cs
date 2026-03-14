using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.CostIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo CostIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de ServiceCostProfile.
/// </summary>
public sealed class ServiceCostProfile : AuditableEntity<ServiceCostProfileId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ServiceCostProfile() { }
}

/// <summary>Identificador fortemente tipado de ServiceCostProfile.</summary>
public sealed record ServiceCostProfileId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ServiceCostProfileId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ServiceCostProfileId From(Guid id) => new(id);
}
