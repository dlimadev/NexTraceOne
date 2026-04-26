using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade DashboardRevision.
/// </summary>
public sealed record DashboardRevisionId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Snapshot imutável de um CustomDashboard num ponto no tempo.
/// Criado automaticamente em cada Update() do dashboard, permitindo histórico,
/// comparação de revisions e revert para qualquer versão anterior.
/// Pertence ao aggregate CustomDashboard — nunca navegado de forma independente.
/// </summary>
public sealed class DashboardRevision : Entity<DashboardRevisionId>
{
    /// <summary>ID do dashboard pai.</summary>
    public CustomDashboardId DashboardId { get; private init; } = null!;

    /// <summary>Número de revisão sequencial (1-based, auto-incrementado).</summary>
    public int RevisionNumber { get; private init; }

    /// <summary>Nome do dashboard no momento da revisão.</summary>
    public string Name { get; private init; } = string.Empty;

    /// <summary>Descrição do dashboard no momento da revisão.</summary>
    public string? Description { get; private init; }

    /// <summary>Layout do dashboard no momento da revisão.</summary>
    public string Layout { get; private init; } = string.Empty;

    /// <summary>Widgets serializados como JSON (snapshot imutável).</summary>
    public string WidgetsJson { get; private init; } = "[]";

    /// <summary>Variáveis serializadas como JSON (snapshot imutável).</summary>
    public string VariablesJson { get; private init; } = "[]";

    /// <summary>Utilizador que causou a revisão.</summary>
    public string AuthorUserId { get; private init; } = string.Empty;

    /// <summary>Descrição opcional da alteração (mensagem de commit).</summary>
    public string? ChangeNote { get; private init; }

    /// <summary>Data/hora UTC em que a revisão foi criada.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Tenant ao qual este snapshot pertence (para RLS).</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Construtor privado para EF Core.</summary>
    private DashboardRevision() { }

    /// <summary>
    /// Cria um novo snapshot de revisão a partir do estado atual do dashboard.
    /// </summary>
    public static DashboardRevision Create(
        CustomDashboardId dashboardId,
        int revisionNumber,
        string name,
        string? description,
        string layout,
        string widgetsJson,
        string variablesJson,
        string authorUserId,
        string tenantId,
        DateTimeOffset now,
        string? changeNote = null)
    {
        Guard.Against.Null(dashboardId, nameof(dashboardId));
        Guard.Against.NegativeOrZero(revisionNumber, nameof(revisionNumber));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(layout, nameof(layout));
        Guard.Against.NullOrWhiteSpace(authorUserId, nameof(authorUserId));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));

        return new DashboardRevision
        {
            Id = new DashboardRevisionId(Guid.NewGuid()),
            DashboardId = dashboardId,
            RevisionNumber = revisionNumber,
            Name = name,
            Description = description,
            Layout = layout,
            WidgetsJson = widgetsJson,
            VariablesJson = variablesJson,
            AuthorUserId = authorUserId,
            ChangeNote = changeNote?.Trim(),
            TenantId = tenantId,
            CreatedAt = now
        };
    }
}
