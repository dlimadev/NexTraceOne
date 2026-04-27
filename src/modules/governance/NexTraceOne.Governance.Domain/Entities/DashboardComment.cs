using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para DashboardComment.</summary>
public sealed record DashboardCommentId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Comentário ancorado num widget de um dashboard (V3.7 — Real-time Collaboration).
/// Suporta threads de discussão com resolução e @menções.
/// </summary>
public sealed class DashboardComment : Entity<DashboardCommentId>
{
    /// <summary>Dashboard ao qual o comentário pertence.</summary>
    public Guid DashboardId { get; private init; }

    /// <summary>Widget ao qual o comentário está ancorado (nullable = comentário de nível de dashboard).</summary>
    public string? WidgetId { get; private init; }

    /// <summary>Tenant do utilizador.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Utilizador autor do comentário.</summary>
    public string AuthorUserId { get; private init; } = string.Empty;

    /// <summary>Conteúdo do comentário (suporta @menções no formato @userId).</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Menções extraídas do conteúdo (JSON array de userIds).</summary>
    public string MentionsJson { get; private set; } = "[]";

    /// <summary>ID do comentário pai em caso de resposta (nullable = thread raiz).</summary>
    public Guid? ParentCommentId { get; private init; }

    /// <summary>Indica se o thread foi resolvido.</summary>
    public bool IsResolved { get; private set; }

    /// <summary>Utilizador que resolveu o thread (nullable).</summary>
    public string? ResolvedByUserId { get; private set; }

    /// <summary>Data/hora UTC de resolução (nullable).</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última edição (nullable).</summary>
    public DateTimeOffset? EditedAt { get; private set; }

    private DashboardComment() { }

    public static DashboardComment Create(
        Guid dashboardId,
        string tenantId,
        string authorUserId,
        string content,
        DateTimeOffset now,
        string? widgetId = null,
        Guid? parentCommentId = null,
        IReadOnlyList<string>? mentions = null)
    {
        Guard.Against.Default(dashboardId, nameof(dashboardId));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(authorUserId, nameof(authorUserId));
        Guard.Against.NullOrWhiteSpace(content, nameof(content));
        Guard.Against.StringTooLong(content, 2000, nameof(content));

        var mentionsJson = mentions is { Count: > 0 }
            ? System.Text.Json.JsonSerializer.Serialize(mentions)
            : "[]";

        return new DashboardComment
        {
            Id = new DashboardCommentId(Guid.NewGuid()),
            DashboardId = dashboardId,
            TenantId = tenantId,
            AuthorUserId = authorUserId,
            WidgetId = widgetId,
            Content = content,
            MentionsJson = mentionsJson,
            ParentCommentId = parentCommentId,
            CreatedAt = now
        };
    }

    public void Edit(string content, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(content, nameof(content));
        Guard.Against.StringTooLong(content, 2000, nameof(content));
        Content = content;
        EditedAt = now;
    }

    public void Resolve(string userId, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        IsResolved = true;
        ResolvedByUserId = userId;
        ResolvedAt = now;
    }

    public void Unresolve()
    {
        IsResolved = false;
        ResolvedByUserId = null;
        ResolvedAt = null;
    }
}
