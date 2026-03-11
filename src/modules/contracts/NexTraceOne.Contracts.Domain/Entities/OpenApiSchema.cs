using NexTraceOne.BuildingBlocks.Domain;
using NexTraceOne.BuildingBlocks.Domain.Primitives;

namespace NexTraceOne.Contracts.Domain.Entities;

/// <summary>
/// Aggregate Root / Entidade do módulo Contracts.
/// TODO: Implementar regras de domínio, invariantes e domain events de OpenApiSchema.
/// </summary>
public sealed class OpenApiSchema : AuditableEntity<OpenApiSchemaId>
{
    // TODO: Implementar propriedades, construtor privado e factory methods
    private OpenApiSchema() { }
}

/// <summary>Identificador fortemente tipado de OpenApiSchema.</summary>
public sealed record OpenApiSchemaId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static OpenApiSchemaId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static OpenApiSchemaId From(Guid id) => new(id);
}
