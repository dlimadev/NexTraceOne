using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.AIKnowledge.Domain.Governance.Entities;

/// <summary>
/// PromptAsset — prompt gerido como contrato: versionado, auditável e comparável.
/// Cada asset possui um slug único (imutável após criação) e uma coleção de PromptVersion.
/// O asset activo expõe sempre o conteúdo da versão com maior número marcada como activa.
/// </summary>
public sealed class PromptAsset : AuditableEntity<PromptAssetId>
{
    private readonly List<PromptVersion> _versions = [];

    private PromptAsset() { }

    /// <summary>Identificador legível único (ex: "incident-root-cause-v2").</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Nome de exibição.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição funcional do asset.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Categoria: system | few-shot | rag | instruction | chain-of-thought.</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>Tags para descoberta (CSV).</summary>
    public string Tags { get; private set; } = string.Empty;

    /// <summary>Variáveis esperadas no conteúdo (CSV, ex: "serviceId,environment").</summary>
    public string Variables { get; private set; } = string.Empty;

    /// <summary>Tenant proprietário do asset (null = platform-level).</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Utilizador que registou o asset.</summary>
    public new string CreatedBy { get; private set; } = string.Empty;

    /// <summary>Número da versão activa actualmente.</summary>
    public int CurrentVersionNumber { get; private set; }

    /// <summary>Indica se o asset está activo (pode ser usado em prompts).</summary>
    public bool IsActive { get; private set; }

    /// <summary>Versões do asset (navegação EF Core).</summary>
    public IReadOnlyList<PromptVersion> Versions => _versions.AsReadOnly();

    public static PromptAsset Create(
        string slug,
        string name,
        string description,
        string category,
        string initialContent,
        string variables,
        string tags,
        Guid? tenantId,
        string createdBy)
    {
        Guard.Against.NullOrWhiteSpace(slug);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.NullOrWhiteSpace(category);
        Guard.Against.NullOrWhiteSpace(initialContent);
        Guard.Against.NullOrWhiteSpace(createdBy);

        var asset = new PromptAsset
        {
            Id = PromptAssetId.New(),
            Slug = slug.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Category = category.Trim(),
            Variables = variables?.Trim() ?? string.Empty,
            Tags = tags?.Trim() ?? string.Empty,
            TenantId = tenantId,
            CreatedBy = createdBy.Trim(),
            CurrentVersionNumber = 1,
            IsActive = true,
        };

        var v1 = PromptVersion.Create(
            asset.Id, versionNumber: 1, content: initialContent,
            changeNotes: "Initial version.", evalScore: null, createdBy: createdBy);
        asset._versions.Add(v1);

        return asset;
    }

    /// <summary>
    /// Adiciona uma nova versão ao asset e actualiza CurrentVersionNumber.
    /// </summary>
    public PromptVersion AddVersion(string content, string changeNotes, string createdBy, decimal? evalScore = null)
    {
        Guard.Against.NullOrWhiteSpace(content);
        Guard.Against.NullOrWhiteSpace(createdBy);

        var nextNumber = CurrentVersionNumber + 1;
        var version = PromptVersion.Create(Id, nextNumber, content, changeNotes, evalScore, createdBy);
        _versions.Add(version);
        CurrentVersionNumber = nextNumber;
        return version;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

/// <summary>Identificador fortemente tipado de PromptAsset.</summary>
public sealed record PromptAssetId(Guid Value) : TypedIdBase(Value)
{
    public static PromptAssetId New() => new(Guid.NewGuid());
    public static PromptAssetId From(Guid id) => new(id);
}
