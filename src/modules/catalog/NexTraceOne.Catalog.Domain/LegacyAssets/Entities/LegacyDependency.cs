using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.LegacyAssets.Enums;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Entities;

/// <summary>
/// Dependência entre ativos legacy — representa uma relação entre dois ativos no catálogo.
/// Suporta dependências entre programas, transações, copybooks, etc.
/// </summary>
public sealed class LegacyDependency : Entity<LegacyDependencyId>
{
    private LegacyDependency() { }

    // ── Relação ───────────────────────────────────────────────────────

    /// <summary>Id do ativo de origem da dependência.</summary>
    public Guid SourceAssetId { get; private set; }

    /// <summary>Tipo do ativo de origem.</summary>
    public MainframeAssetType SourceAssetType { get; private set; }

    /// <summary>Id do ativo de destino da dependência.</summary>
    public Guid TargetAssetId { get; private set; }

    /// <summary>Tipo do ativo de destino.</summary>
    public MainframeAssetType TargetAssetType { get; private set; }

    /// <summary>Tipo de dependência (ex.: CALLS, READS, WRITES, INCLUDES).</summary>
    public string DependencyType { get; private set; } = string.Empty;

    /// <summary>Descrição adicional da dependência.</summary>
    public string Description { get; private set; } = string.Empty;

    // ── Auditoria ─────────────────────────────────────────────────────

    /// <summary>Data de descoberta da dependência.</summary>
    public DateTimeOffset DiscoveredAt { get; private init; }

    // ── Concorrência ──────────────────────────────────────────────────

    /// <summary>
    /// Token de concorrência otimista (PostgreSQL xmin).
    /// </summary>
    public uint RowVersion { get; set; }

    // ── Factory method ────────────────────────────────────────────────

    /// <summary>Cria uma nova dependência entre ativos legacy.</summary>
    public static LegacyDependency Create(
        Guid sourceAssetId, MainframeAssetType sourceAssetType,
        Guid targetAssetId, MainframeAssetType targetAssetType,
        string dependencyType)
    {
        Guard.Against.Default(sourceAssetId);
        Guard.Against.Default(targetAssetId);
        Guard.Against.NullOrWhiteSpace(dependencyType);

        return new LegacyDependency
        {
            Id = LegacyDependencyId.New(),
            SourceAssetId = sourceAssetId,
            SourceAssetType = sourceAssetType,
            TargetAssetId = targetAssetId,
            TargetAssetType = targetAssetType,
            DependencyType = dependencyType.Trim(),
            DiscoveredAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Atualiza a descrição da dependência.</summary>
    public void UpdateDescription(string description)
    {
        Description = description ?? string.Empty;
    }
}

/// <summary>Identificador fortemente tipado de LegacyDependency.</summary>
public sealed record LegacyDependencyId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static LegacyDependencyId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static LegacyDependencyId From(Guid id) => new(id);
}
