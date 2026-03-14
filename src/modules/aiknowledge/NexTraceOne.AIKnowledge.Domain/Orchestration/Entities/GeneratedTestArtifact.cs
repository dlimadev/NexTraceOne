using NexTraceOne.BuildingBlocks.Core;
using NexTraceOne.BuildingBlocks.Core.Primitives;

namespace NexTraceOne.AiOrchestration.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo AiOrchestration.
/// TODO: Implementar regras de domínio, invariantes e domain events de GeneratedTestArtifact.
/// </summary>
public sealed class GeneratedTestArtifact : AuditableEntity<GeneratedTestArtifactId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private GeneratedTestArtifact() { }
}

/// <summary>Identificador fortemente tipado de GeneratedTestArtifact.</summary>
public sealed record GeneratedTestArtifactId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static GeneratedTestArtifactId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static GeneratedTestArtifactId From(Guid id) => new(id);
}
