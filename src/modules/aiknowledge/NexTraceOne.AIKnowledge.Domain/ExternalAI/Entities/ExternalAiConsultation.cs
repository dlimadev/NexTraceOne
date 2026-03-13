using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.ExternalAi.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo ExternalAi.
/// TODO: Implementar regras de domínio, invariantes e domain events de ExternalAiConsultation.
/// </summary>
public sealed class ExternalAiConsultation : AuditableEntity<ExternalAiConsultationId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ExternalAiConsultation() { }
}

/// <summary>Identificador fortemente tipado de ExternalAiConsultation.</summary>
public sealed record ExternalAiConsultationId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ExternalAiConsultationId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ExternalAiConsultationId From(Guid id) => new(id);
}
