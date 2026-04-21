using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Knowledge.Domain.Enums;

namespace NexTraceOne.Knowledge.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para KnowledgeDocument.
/// </summary>
public sealed record KnowledgeDocumentId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Agregado que representa um documento de conhecimento no Knowledge Hub.
/// Um KnowledgeDocument é a unidade central de conhecimento operacional e técnico
/// da plataforma NexTraceOne — pode representar documentação, guias, runbooks,
/// post-mortems, procedimentos ou referências.
///
/// Owner: módulo Knowledge.
/// Pilar: Source of Truth &amp; Operational Knowledge.
/// </summary>
public sealed class KnowledgeDocument : Entity<KnowledgeDocumentId>
{
    /// <summary>Título do documento.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Slug para URL amigável (gerado a partir do título).</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Conteúdo do documento em Markdown.</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Resumo ou descrição curta do documento.</summary>
    public string? Summary { get; private set; }

    /// <summary>Categoria do documento.</summary>
    public DocumentCategory Category { get; private set; }

    /// <summary>Estado do ciclo de vida do documento.</summary>
    public DocumentStatus Status { get; private set; }

    /// <summary>Tags para classificação e pesquisa (armazenadas como JSON).</summary>
    public IReadOnlyList<string> Tags { get; private set; } = [];

    /// <summary>Identificador do autor (UserId).</summary>
    public Guid AuthorId { get; private init; }

    /// <summary>Identificador do último editor (UserId).</summary>
    public Guid? LastEditorId { get; private set; }

    /// <summary>Versão do documento (incrementada a cada edição).</summary>
    public int Version { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Data/hora UTC de publicação.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Score de frescura do documento (0–100). 100 = totalmente fresco.</summary>
    public int FreshnessScore { get; private set; } = 100;

    /// <summary>Data da última revisão do documento.</summary>
    public DateTimeOffset? LastReviewedAt { get; private set; }

    /// <summary>Utilizador que fez a última revisão.</summary>
    public string? ReviewedBy { get; private set; }

    private KnowledgeDocument() { }

    /// <summary>Cria um novo documento de conhecimento.</summary>
    public static KnowledgeDocument Create(
        string title,
        string content,
        string? summary,
        DocumentCategory category,
        IReadOnlyList<string>? tags,
        Guid authorId,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.StringTooLong(title, 500, nameof(title));
        Guard.Against.NullOrWhiteSpace(content, nameof(content));
        Guard.Against.Default(authorId, nameof(authorId));

        return new KnowledgeDocument
        {
            Id = new KnowledgeDocumentId(Guid.NewGuid()),
            Title = title.Trim(),
            Slug = GenerateSlug(title),
            Content = content,
            Summary = summary?.Trim(),
            Category = category,
            Status = DocumentStatus.Draft,
            Tags = tags ?? [],
            AuthorId = authorId,
            Version = 1,
            CreatedAt = utcNow
        };
    }

    /// <summary>Atualiza o conteúdo do documento.</summary>
    public void UpdateContent(string title, string content, string? summary, Guid editorId, DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.StringTooLong(title, 500, nameof(title));
        Guard.Against.NullOrWhiteSpace(content, nameof(content));
        Guard.Against.Default(editorId, nameof(editorId));

        Title = title.Trim();
        Slug = GenerateSlug(title);
        Content = content;
        Summary = summary?.Trim();
        LastEditorId = editorId;
        Version++;
        UpdatedAt = utcNow;
    }

    /// <summary>Atualiza as tags do documento.</summary>
    public void UpdateTags(IReadOnlyList<string> tags, DateTimeOffset utcNow)
    {
        Tags = tags;
        UpdatedAt = utcNow;
    }

    /// <summary>Atualiza a categoria do documento.</summary>
    public void UpdateCategory(DocumentCategory category, DateTimeOffset utcNow)
    {
        Category = category;
        UpdatedAt = utcNow;
    }

    /// <summary>Publica o documento tornando-o visível.</summary>
    public void Publish(DateTimeOffset utcNow)
    {
        Status = DocumentStatus.Published;
        PublishedAt = utcNow;
        UpdatedAt = utcNow;
    }

    /// <summary>Arquiva o documento.</summary>
    public void Archive(DateTimeOffset utcNow)
    {
        Status = DocumentStatus.Archived;
        UpdatedAt = utcNow;
    }

    /// <summary>Marca o documento como obsoleto.</summary>
    public void Deprecate(DateTimeOffset utcNow)
    {
        Status = DocumentStatus.Deprecated;
        UpdatedAt = utcNow;
    }

    /// <summary>Reverte o documento para rascunho.</summary>
    public void RevertToDraft(DateTimeOffset utcNow)
    {
        Status = DocumentStatus.Draft;
        UpdatedAt = utcNow;
    }

    /// <summary>
    /// Calcula e atualiza o FreshnessScore com base na idade do documento
    /// e no tempo desde a última revisão.
    /// Algoritmo: Score decresce linearmente ao longo de 180 dias sem revisão.
    /// </summary>
    public void ComputeFreshnessScore(DateTimeOffset now)
    {
        var referenceDate = LastReviewedAt ?? UpdatedAt ?? CreatedAt;
        var daysSinceReview = (now - referenceDate).TotalDays;
        FreshnessScore = Math.Max(0, (int)Math.Round(100 - daysSinceReview / 180.0 * 100));
    }

    /// <summary>Marca o documento como revisto.</summary>
    public void MarkReviewed(string reviewedBy, DateTimeOffset at)
    {
        ReviewedBy = reviewedBy;
        LastReviewedAt = at;
        FreshnessScore = 100;
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.Trim().ToLowerInvariant();

        // Replace spaces and common separators with dashes
        slug = slug.Replace(' ', '-')
                   .Replace('_', '-')
                   .Replace('.', '-');

        // Remove characters that are not alphanumeric or dashes
        var builder = new System.Text.StringBuilder(slug.Length);
        foreach (var c in slug)
        {
            if (char.IsLetterOrDigit(c) || c == '-')
            {
                builder.Append(c);
            }
        }

        slug = builder.ToString();

        // Collapse consecutive dashes
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        // Trim leading/trailing dashes
        return slug.Trim('-');
    }
}
