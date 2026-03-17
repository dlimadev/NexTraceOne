using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Domain.Contracts.Entities;

/// <summary>
/// Entidade que representa uma entidade Canonical no módulo de contratos.
/// Entidades Canonical são schemas/modelos padronizados e reutilizáveis
/// que servem como fonte da verdade para payloads, request/response bodies,
/// eventos e DTOs partilhados entre contratos.
/// Permite governar a reutilização, prevenir duplicação e garantir consistência.
/// </summary>
public sealed class CanonicalEntity : AuditableEntity<CanonicalEntityId>
{
    private readonly List<string> _aliases = [];
    private readonly List<string> _tags = [];

    private CanonicalEntity() { }

    /// <summary>Nome técnico da entidade Canonical (ex: "CustomerAddress", "OrderLineItem").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição funcional da entidade.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Domínio de negócio ao qual pertence (ex: "payments", "customer").</summary>
    public string Domain { get; private set; } = string.Empty;

    /// <summary>Categoria funcional (ex: "entity", "event-payload", "dto", "enum").</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Owner responsável pela entidade.</summary>
    public string Owner { get; private set; } = string.Empty;

    /// <summary>Versão actual da entidade Canonical.</summary>
    public string Version { get; private set; } = "1.0.0";

    /// <summary>Estado da entidade no ciclo de vida.</summary>
    public CanonicalEntityState State { get; private set; }

    /// <summary>Schema/model da entidade em JSON Schema format.</summary>
    public string SchemaContent { get; private set; } = string.Empty;

    /// <summary>Formato do schema: "json-schema", "avro", "protobuf".</summary>
    public string SchemaFormat { get; private set; } = "json-schema";

    /// <summary>Nomes alternativos conhecidos desta entidade.</summary>
    public IReadOnlyList<string> Aliases => _aliases.AsReadOnly();

    /// <summary>Tags para categorização e pesquisa.</summary>
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    /// <summary>Nível de criticidade da entidade.</summary>
    public string Criticality { get; private set; } = "Medium";

    /// <summary>Política de reutilização: "mandatory", "recommended", "optional".</summary>
    public string ReusePolicy { get; private set; } = "recommended";

    /// <summary>Organização/tenant owner.</summary>
    public string? OrganizationId { get; private set; }

    /// <summary>Número de contratos conhecidos que usam esta entidade.</summary>
    public int KnownUsageCount { get; private set; }

    /// <summary>Cria nova entidade Canonical.</summary>
    public static CanonicalEntity Create(
        string name,
        string description,
        string domain,
        string category,
        string owner,
        string schemaContent,
        string? schemaFormat = null,
        string? criticality = null,
        string? reusePolicy = null,
        string? organizationId = null,
        IEnumerable<string>? aliases = null,
        IEnumerable<string>? tags = null)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(domain);
        Guard.Against.NullOrWhiteSpace(owner);
        Guard.Against.NullOrWhiteSpace(schemaContent);

        var entity = new CanonicalEntity
        {
            Id = CanonicalEntityId.New(),
            Name = name,
            Description = description ?? string.Empty,
            Domain = domain,
            Category = category ?? "entity",
            Owner = owner,
            Version = "1.0.0",
            State = CanonicalEntityState.Draft,
            SchemaContent = schemaContent,
            SchemaFormat = schemaFormat ?? "json-schema",
            Criticality = criticality ?? "Medium",
            ReusePolicy = reusePolicy ?? "recommended",
            OrganizationId = organizationId,
            KnownUsageCount = 0
        };

        if (aliases is not null)
            entity._aliases.AddRange(aliases);
        if (tags is not null)
            entity._tags.AddRange(tags);

        return entity;
    }

    /// <summary>Atualiza o schema da entidade e incrementa a versão.</summary>
    public void UpdateSchema(string schemaContent, string newVersion)
    {
        Guard.Against.NullOrWhiteSpace(schemaContent);
        Guard.Against.NullOrWhiteSpace(newVersion);
        SchemaContent = schemaContent;
        Version = newVersion;
    }

    /// <summary>Atualiza a metadata da entidade.</summary>
    public void UpdateMetadata(
        string name,
        string description,
        string domain,
        string category,
        string owner,
        string? criticality = null,
        string? reusePolicy = null)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Name = name;
        Description = description ?? string.Empty;
        Domain = domain;
        Category = category ?? Category;
        Owner = owner;
        Criticality = criticality ?? Criticality;
        ReusePolicy = reusePolicy ?? ReusePolicy;
    }

    /// <summary>Publica a entidade para reutilização.</summary>
    public void Publish()
    {
        State = CanonicalEntityState.Published;
    }

    /// <summary>Deprecia a entidade.</summary>
    public void Deprecate()
    {
        State = CanonicalEntityState.Deprecated;
    }

    /// <summary>Aposenta a entidade.</summary>
    public void Retire()
    {
        State = CanonicalEntityState.Retired;
    }

    /// <summary>Incrementa o contador de uso conhecido.</summary>
    public void IncrementUsageCount() => KnownUsageCount++;

    /// <summary>Decrementa o contador de uso conhecido.</summary>
    public void DecrementUsageCount() => KnownUsageCount = Math.Max(0, KnownUsageCount - 1);

    /// <summary>Actualiza os aliases.</summary>
    public void SetAliases(IEnumerable<string> aliases)
    {
        _aliases.Clear();
        _aliases.AddRange(aliases);
    }

    /// <summary>Actualiza as tags.</summary>
    public void SetTags(IEnumerable<string> tags)
    {
        _tags.Clear();
        _tags.AddRange(tags);
    }
}

/// <summary>Identificador fortemente tipado de CanonicalEntity.</summary>
public sealed record CanonicalEntityId(Guid Value) : TypedIdBase(Value)
{
    public static CanonicalEntityId New() => new(Guid.NewGuid());
    public static CanonicalEntityId From(Guid value) => new(value);
}
