using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Domain.Graph.Entities;

/// <summary>
/// Link categorizado associado a um serviço do catálogo.
/// Substitui os campos limitados DocumentationUrl/RepositoryUrl por um modelo
/// extensível que suporta múltiplos links por categoria (repositório, CI/CD,
/// monitorização, wiki, runbooks, ADRs, etc.).
/// </summary>
public sealed class ServiceLink : Entity<ServiceLinkId>
{
    private ServiceLink() { }

    /// <summary>Identificador do serviço ao qual este link pertence.</summary>
    public ServiceAssetId ServiceAssetId { get; private set; } = default!;

    /// <summary>Categoria semântica do link.</summary>
    public LinkCategory Category { get; private set; }

    /// <summary>Título legível do link.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>URL de destino do link.</summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>Descrição opcional do link.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Hint de ícone opcional para renderização customizada na UI.</summary>
    public string IconHint { get; private set; } = string.Empty;

    /// <summary>Ordem de apresentação dentro da categoria.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Data de criação do link (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Navegação para o serviço associado.</summary>
    public ServiceAsset? ServiceAsset { get; private set; }

    // ── Factory method ────────────────────────────────────────────────

    /// <summary>Cria um novo link associado a um serviço.</summary>
    public static ServiceLink Create(
        ServiceAssetId serviceAssetId,
        LinkCategory category,
        string title,
        string url,
        string? description = null,
        string? iconHint = null,
        int sortOrder = 0)
        => new()
        {
            Id = ServiceLinkId.New(),
            ServiceAssetId = Guard.Against.Default(serviceAssetId),
            Category = category,
            Title = Guard.Against.NullOrWhiteSpace(title),
            Url = Guard.Against.NullOrWhiteSpace(url),
            Description = description ?? string.Empty,
            IconHint = iconHint ?? string.Empty,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };

    // ── Mutações controladas ──────────────────────────────────────────

    /// <summary>Atualiza os dados do link.</summary>
    public void Update(
        LinkCategory category,
        string title,
        string url,
        string? description = null,
        string? iconHint = null,
        int sortOrder = 0)
    {
        Category = category;
        Title = Guard.Against.NullOrWhiteSpace(title);
        Url = Guard.Against.NullOrWhiteSpace(url);
        Description = description ?? string.Empty;
        IconHint = iconHint ?? string.Empty;
        SortOrder = sortOrder;
    }
}

/// <summary>Identificador fortemente tipado de ServiceLink.</summary>
public sealed record ServiceLinkId(Guid Value) : TypedIdBase(Value)
{
    /// <summary>Cria novo Id com Guid gerado automaticamente.</summary>
    public static ServiceLinkId New() => new(Guid.NewGuid());

    /// <summary>Cria Id a partir de Guid existente.</summary>
    public static ServiceLinkId From(Guid id) => new(id);
}
