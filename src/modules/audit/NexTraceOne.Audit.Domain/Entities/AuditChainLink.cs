using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Audit.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Audit.
/// TODO: Implementar regras de domínio, invariantes e domain events de AuditChainLink.
/// </summary>
public sealed class AuditChainLink : AuditableEntity<AuditChainLinkId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private AuditChainLink() { }
}

/// <summary>Identificador fortemente tipado de AuditChainLink.</summary>
public sealed record AuditChainLinkId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static AuditChainLinkId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static AuditChainLinkId From(Guid id) => new(id);
}
