using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.ExternalAi.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo ExternalAi.
/// TODO: Implementar regras de domínio, invariantes e domain events de ExternalAiPolicy.
/// </summary>
public sealed class ExternalAiPolicy : AuditableEntity<ExternalAiPolicyId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ExternalAiPolicy() { }
}

/// <summary>Identificador fortemente tipado de ExternalAiPolicy.</summary>
public sealed record ExternalAiPolicyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ExternalAiPolicyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ExternalAiPolicyId From(Guid id) => new(id);
}
