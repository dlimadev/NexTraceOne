using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Audit.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Audit.
/// TODO: Implementar regras de domínio, invariantes e domain events de RetentionPolicy.
/// </summary>
public sealed class RetentionPolicy : AuditableEntity<RetentionPolicyId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private RetentionPolicy() { }
}

/// <summary>Identificador fortemente tipado de RetentionPolicy.</summary>
public sealed record RetentionPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static RetentionPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static RetentionPolicyId From(Guid id) => new(id);
}
