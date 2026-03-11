using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.RuntimeIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo RuntimeIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de ObservabilityProfile.
/// </summary>
public sealed class ObservabilityProfile : AuditableEntity<ObservabilityProfileId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ObservabilityProfile() { }
}

/// <summary>Identificador fortemente tipado de ObservabilityProfile.</summary>
public sealed record ObservabilityProfileId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ObservabilityProfileId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ObservabilityProfileId From(Guid id) => new(id);
}
