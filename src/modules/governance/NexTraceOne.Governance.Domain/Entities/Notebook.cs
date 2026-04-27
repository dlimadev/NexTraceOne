using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para Notebook.</summary>
public sealed record NotebookId(Guid Value) : TypedIdBase(Value);

/// <summary>Identificador fortemente tipado para NotebookCell.</summary>
public sealed record NotebookCellId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Célula de um Notebook — polimorfismo via CellType discriminator.
/// Serializada como JSONB em gov_notebook_cells.
/// </summary>
public sealed record NotebookCell(
    NotebookCellId Id,
    NotebookCellType CellType,
    int SortOrder,
    string Content,      // markdown text, NQL query, widget type key, action spec, ai prompt
    string? OutputJson,  // rendered output (nullable — updated on execute)
    bool IsCollapsed,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Notebook — superfície de investigação operacional e post-mortems.
/// Aggregate root de células ordenadas (Markdown, Query, Widget, Action, AI).
/// Versionado e partilhado com a mesma SharingPolicy dos dashboards.
/// Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// </summary>
public sealed class Notebook : Entity<NotebookId>
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string TenantId { get; private init; } = string.Empty;
    public string CreatedByUserId { get; private init; } = string.Empty;
    public string? TeamId { get; private set; }
    public string Persona { get; private set; } = string.Empty;

    /// <summary>Células ordenadas da notebook (JSONB).</summary>
    public IReadOnlyList<NotebookCell> Cells { get; private set; } = [];

    /// <summary>Política de partilha (reutiliza o mesmo VO dos dashboards).</summary>
    public SharingPolicy SharingPolicy { get; private set; } = SharingPolicy.Private;

    /// <summary>Número de revisão atual (auto-incrementado em cada Update).</summary>
    public int CurrentRevisionNumber { get; private set; }

    /// <summary>Estado do ciclo de vida da notebook.</summary>
    public NotebookStatus Status { get; private set; } = NotebookStatus.Draft;

    /// <summary>Opcional: linked dashboard ID para cross-link.</summary>
    public CustomDashboardId? LinkedDashboardId { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public uint RowVersion { get; set; }

    private Notebook() { }

    public static Notebook Create(
        string title,
        string? description,
        string tenantId,
        string userId,
        string persona,
        DateTimeOffset now,
        string? teamId = null)
    {
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.StringTooLong(title, 200, nameof(title));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(persona, nameof(persona));

        if (description is not null)
            Guard.Against.StringTooLong(description, 1000, nameof(description));

        return new Notebook
        {
            Id = new NotebookId(Guid.NewGuid()),
            Title = title.Trim(),
            Description = description?.Trim(),
            TenantId = tenantId,
            CreatedByUserId = userId,
            TeamId = teamId,
            Persona = persona,
            Cells = [],
            SharingPolicy = SharingPolicy.Private,
            Status = NotebookStatus.Draft,
            CurrentRevisionNumber = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string title,
        string? description,
        IReadOnlyList<NotebookCell> cells,
        string? teamId,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.StringTooLong(title, 200, nameof(title));
        if (description is not null)
            Guard.Against.StringTooLong(description, 1000, nameof(description));

        Title = title.Trim();
        Description = description?.Trim();
        Cells = cells;
        TeamId = teamId;
        CurrentRevisionNumber++;
        UpdatedAt = now;
    }

    public void AddCell(NotebookCell cell, DateTimeOffset now)
    {
        Guard.Against.Null(cell, nameof(cell));
        var updated = Cells.ToList();
        updated.Add(cell);
        Cells = updated;
        CurrentRevisionNumber++;
        UpdatedAt = now;
    }

    public void UpdateCellOutput(NotebookCellId cellId, string outputJson, DateTimeOffset now)
    {
        var updated = Cells.Select(c =>
            c.Id == cellId
                ? c with { OutputJson = outputJson, UpdatedAt = now }
                : c
        ).ToList();
        Cells = updated;
        UpdatedAt = now;
    }

    public void SetSharingPolicy(SharingPolicy policy, DateTimeOffset now)
    {
        Guard.Against.Null(policy, nameof(policy));
        SharingPolicy = policy;
        UpdatedAt = now;
    }

    public void Publish(DateTimeOffset now)
    {
        Status = NotebookStatus.Published;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = NotebookStatus.Archived;
        UpdatedAt = now;
    }

    public void LinkDashboard(CustomDashboardId dashboardId, DateTimeOffset now)
    {
        Guard.Against.Null(dashboardId, nameof(dashboardId));
        LinkedDashboardId = dashboardId;
        UpdatedAt = now;
    }

    public static NotebookCell CreateMarkdownCell(int sortOrder, string text, DateTimeOffset now)
        => new NotebookCell(
            new NotebookCellId(Guid.NewGuid()),
            NotebookCellType.Markdown,
            sortOrder,
            text,
            OutputJson: null,
            IsCollapsed: false,
            CreatedAt: now,
            UpdatedAt: now);

    public static NotebookCell CreateQueryCell(int sortOrder, string nql, DateTimeOffset now)
        => new NotebookCell(
            new NotebookCellId(Guid.NewGuid()),
            NotebookCellType.Query,
            sortOrder,
            nql,
            OutputJson: null,
            IsCollapsed: false,
            CreatedAt: now,
            UpdatedAt: now);

    public static NotebookCell CreateWidgetCell(int sortOrder, string widgetType, DateTimeOffset now)
        => new NotebookCell(
            new NotebookCellId(Guid.NewGuid()),
            NotebookCellType.Widget,
            sortOrder,
            widgetType,
            OutputJson: null,
            IsCollapsed: false,
            CreatedAt: now,
            UpdatedAt: now);

    public static NotebookCell CreateAiCell(int sortOrder, string prompt, DateTimeOffset now)
        => new NotebookCell(
            new NotebookCellId(Guid.NewGuid()),
            NotebookCellType.Ai,
            sortOrder,
            prompt,
            OutputJson: null,
            IsCollapsed: false,
            CreatedAt: now,
            UpdatedAt: now);
}
