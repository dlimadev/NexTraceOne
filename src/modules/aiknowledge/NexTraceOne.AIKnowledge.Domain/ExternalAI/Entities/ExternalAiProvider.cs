using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.ExternalAi.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo ExternalAi.
/// TODO: Implementar regras de domínio, invariantes e domain events de ExternalAiProvider.
/// </summary>
public sealed class ExternalAiProvider : AuditableEntity<ExternalAiProviderId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private ExternalAiProvider() { }
}

/// <summary>Identificador fortemente tipado de ExternalAiProvider.</summary>
public sealed record ExternalAiProviderId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ExternalAiProviderId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ExternalAiProviderId From(Guid id) => new(id);
}
