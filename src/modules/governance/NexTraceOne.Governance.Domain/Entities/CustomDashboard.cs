using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade CustomDashboard.
/// </summary>
public sealed record CustomDashboardId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Representa um dashboard customizado associado a uma persona e tenant.
/// Permite que cada utilizador ou equipa configure vistas personalizadas
/// de métricas, scorecards, change confidence e governança.
/// Aggregate root com ciclo de vida próprio (criação, modificação, clonagem).
/// </summary>
public sealed class CustomDashboard : Entity<CustomDashboardId>
{
    /// <summary>Nome legível do dashboard (máx. 100 caracteres).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Descrição opcional do dashboard.</summary>
    public string? Description { get; private set; }

    /// <summary>Layout do dashboard (ex: "grid", "two-column").</summary>
    public string Layout { get; private set; } = string.Empty;

    /// <summary>Persona alvo do dashboard.</summary>
    public string Persona { get; private set; } = string.Empty;

    /// <summary>Lista de widget IDs configurados no dashboard (JSONB).</summary>
    public IReadOnlyList<string> WidgetIds { get; private set; } = [];

    /// <summary>Indica se o dashboard é partilhado com a equipa.</summary>
    public bool IsShared { get; private set; }

    /// <summary>Identificador do tenant proprietário.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Identificador do utilizador criador.</summary>
    public string CreatedByUserId { get; private init; } = string.Empty;

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última modificação.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

    /// <summary>Construtor privado para EF Core.</summary>
    private CustomDashboard() { }

    /// <summary>
    /// Cria um novo dashboard customizado com validação de invariantes.
    /// </summary>
    public static CustomDashboard Create(
        string name,
        string? description,
        string layout,
        string persona,
        IReadOnlyList<string> widgetIds,
        string tenantId,
        string userId,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 100, nameof(name));
        Guard.Against.NullOrWhiteSpace(layout, nameof(layout));
        Guard.Against.NullOrWhiteSpace(persona, nameof(persona));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        if (description is not null)
            Guard.Against.StringTooLong(description, 500, nameof(description));

        return new CustomDashboard
        {
            Id = new CustomDashboardId(Guid.NewGuid()),
            Name = name.Trim(),
            Description = description?.Trim(),
            Layout = layout.Trim(),
            Persona = persona.Trim(),
            WidgetIds = widgetIds,
            IsShared = false,
            TenantId = tenantId,
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Cria um clone independente a partir deste dashboard.
    /// </summary>
    public CustomDashboard Clone(string newName, string userId, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(newName, nameof(newName));
        Guard.Against.StringTooLong(newName, 100, nameof(newName));

        return new CustomDashboard
        {
            Id = new CustomDashboardId(Guid.NewGuid()),
            Name = newName.Trim(),
            Description = Description,
            Layout = Layout,
            Persona = Persona,
            WidgetIds = WidgetIds.ToList(),
            IsShared = false,
            TenantId = TenantId,
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Atualiza os dados mutáveis do dashboard.
    /// </summary>
    public void Update(string name, string? description, string layout, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 100, nameof(name));
        Guard.Against.NullOrWhiteSpace(layout, nameof(layout));

        if (description is not null)
            Guard.Against.StringTooLong(description, 500, nameof(description));

        Name = name.Trim();
        Description = description?.Trim();
        Layout = layout.Trim();
        UpdatedAt = now;
    }

    /// <summary>
    /// Partilha ou retira a partilha do dashboard.
    /// </summary>
    public void SetShared(bool isShared, DateTimeOffset now)
    {
        IsShared = isShared;
        UpdatedAt = now;
    }
}
