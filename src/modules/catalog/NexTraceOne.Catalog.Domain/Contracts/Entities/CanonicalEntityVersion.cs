using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Registo imutável de uma versão específica de uma entidade canónica.
/// Permite consultar histórico e calcular diff entre versões.
/// </summary>
public sealed class CanonicalEntityVersion : Entity<CanonicalEntityVersionId>
{
    private CanonicalEntityVersion() { }

    /// <summary>Identificador da entidade canónica pai.</summary>
    public CanonicalEntityId CanonicalEntityId { get; private set; } = null!;

    /// <summary>Versão semântica desta snapshot (ex: "1.0.0").</summary>
    public string Version { get; private set; } = string.Empty;

    /// <summary>Conteúdo do schema nesta versão.</summary>
    public string SchemaContent { get; private set; } = string.Empty;

    /// <summary>Formato do schema: "json-schema", "avro", "protobuf".</summary>
    public string SchemaFormat { get; private set; } = "json-schema";

    /// <summary>Descrição da alteração introduzida nesta versão.</summary>
    public string ChangeDescription { get; private set; } = string.Empty;

    /// <summary>Utilizador que publicou esta versão.</summary>
    public string PublishedBy { get; private set; } = string.Empty;

    /// <summary>Data/hora UTC de publicação.</summary>
    public DateTimeOffset PublishedAt { get; private set; }

    /// <summary>Cria uma nova versão imutável de uma entidade canónica.</summary>
    public static CanonicalEntityVersion Create(
        CanonicalEntityId canonicalEntityId,
        string version,
        string schemaContent,
        string schemaFormat,
        string changeDescription,
        string publishedBy)
    {
        Guard.Against.Null(canonicalEntityId);
        Guard.Against.NullOrWhiteSpace(version);
        Guard.Against.NullOrWhiteSpace(schemaContent);
        Guard.Against.NullOrWhiteSpace(publishedBy);

        return new CanonicalEntityVersion
        {
            Id = CanonicalEntityVersionId.New(),
            CanonicalEntityId = canonicalEntityId,
            Version = version,
            SchemaContent = schemaContent,
            SchemaFormat = schemaFormat ?? "json-schema",
            ChangeDescription = changeDescription ?? string.Empty,
            PublishedBy = publishedBy,
            PublishedAt = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>Identificador fortemente tipado de CanonicalEntityVersion.</summary>
public sealed record CanonicalEntityVersionId(Guid Value) : TypedIdBase(Value)
{
    public static CanonicalEntityVersionId New() => new(Guid.NewGuid());
    public static CanonicalEntityVersionId From(Guid id) => new(id);
}
