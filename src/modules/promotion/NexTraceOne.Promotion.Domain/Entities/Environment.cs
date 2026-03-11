using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Promotion.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Promotion.
/// TODO: Implementar regras de domínio, invariantes e domain events de Environment.
/// </summary>
public sealed class Environment : AuditableEntity<EnvironmentId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private Environment() { }
}

/// <summary>Identificador fortemente tipado de Environment.</summary>
public sealed record EnvironmentId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static EnvironmentId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static EnvironmentId From(Guid id) => new(id);
}
