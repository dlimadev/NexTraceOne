using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.ChangeIntelligence.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo ChangeIntelligence.
/// TODO: Implementar regras de domínio, invariantes e domain events de Release.
/// </summary>
public sealed class Release : AuditableEntity<ReleaseId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private Release() { }
}

/// <summary>Identificador fortemente tipado de Release.</summary>
public sealed record ReleaseId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ReleaseId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ReleaseId From(Guid id) => new(id);
}
