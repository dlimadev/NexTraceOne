using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Artefacto DB2 — objeto de base de dados no mainframe (tabela, vista, SP, etc.).
/// </summary>
public sealed class Db2Artifact : Entity<Db2ArtifactId>
{
    private Db2Artifact() { }

    // ── Identidade ────────────────────────────────────────────────────

    /// <summary>Nome do artefacto (identificador técnico).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Nome de apresentação do artefacto.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Descrição do artefacto e a sua finalidade.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Sistema mainframe ao qual o artefacto pertence.</summary>
    public MainframeSystemId SystemId { get; private set; } = null!;

    // ── DB2 ───────────────────────────────────────────────────────────

    /// <summary>Tipo do artefacto DB2 (tabela, vista, SP, etc.).</summary>
    public Db2ArtifactType ArtifactType { get; private set; } = Db2ArtifactType.Table;

    /// <summary>Nome do schema DB2.</summary>
    public string SchemaName { get; private set; } = string.Empty;

    /// <summary>Nome do tablespace DB2.</summary>
    public string TablespaceName { get; private set; } = string.Empty;

    /// <summary>Nome da base de dados DB2.</summary>
    public string DatabaseName { get; private set; } = string.Empty;

    // ── Classificação ─────────────────────────────────────────────────

    /// <summary>Nível de criticidade do artefacto para o negócio.</summary>
    public Criticality Criticality { get; private set; } = Criticality.Medium;

    /// <summary>Estado do ciclo de vida do artefacto.</summary>
    public LifecycleStatus LifecycleStatus { get; private set; } = LifecycleStatus.Active;

    // ── Auditoria ─────────────────────────────────────────────────────

    /// <summary>Data de criação do registo.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    // ── Concorrência ──────────────────────────────────────────────────

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// </summary>
    public uint RowVersion { get; set; }

    // ── Factory method ────────────────────────────────────────────────

    /// <summary>Cria um novo artefacto DB2 com os campos obrigatórios.</summary>
    public static Db2Artifact Create(
        string name, MainframeSystemId systemId, Db2ArtifactType artifactType)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.Null(systemId);

        return new Db2Artifact
        {
            Id = Db2ArtifactId.New(),
            Name = name.Trim(),
            DisplayName = name.Trim(),
            SystemId = systemId,
            ArtifactType = artifactType,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Atualiza detalhes e classificação do artefacto.</summary>
    public void UpdateDetails(
        string displayName, string description,
        string schemaName, string tablespaceName, string databaseName,
        Criticality criticality, LifecycleStatus lifecycleStatus)
    {
        DisplayName = displayName ?? string.Empty;
        Description = description ?? string.Empty;
        SchemaName = schemaName ?? string.Empty;
        TablespaceName = tablespaceName ?? string.Empty;
        DatabaseName = databaseName ?? string.Empty;
        Criticality = criticality;
        LifecycleStatus = lifecycleStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>Identificador fortemente tipado de Db2Artifact.</summary>
public sealed record Db2ArtifactId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static Db2ArtifactId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static Db2ArtifactId From(Guid id) => new(id);
}
