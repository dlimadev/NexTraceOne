using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Workflow.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Workflow.
/// TODO: Implementar regras de domínio, invariantes e domain events de EvidencePack.
/// </summary>
public sealed class EvidencePack : AuditableEntity<EvidencePackId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private EvidencePack() { }
}

/// <summary>Identificador fortemente tipado de EvidencePack.</summary>
public sealed record EvidencePackId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static EvidencePackId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static EvidencePackId From(Guid id) => new(id);
}
