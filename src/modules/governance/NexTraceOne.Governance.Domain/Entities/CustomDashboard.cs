using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para a entidade CustomDashboard.
/// </summary>
public sealed record CustomDashboardId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Posição e dimensão de um widget no grid do dashboard.
/// </summary>
public sealed record WidgetPosition(int X, int Y, int Width, int Height);

/// <summary>
/// Configuração específica de um widget: filtros contextuais, escopo e período.
/// </summary>
public sealed record WidgetConfig(
    string? ServiceId = null,
    string? TeamId = null,
    string? TimeRange = null,
    string? CustomTitle = null);

/// <summary>
/// Widget configurado de um dashboard: tipo, posição no grid e config contextual.
/// Value object serializado como JSONB no PostgreSQL.
/// </summary>
public sealed record DashboardWidget(
    string WidgetId,
    string Type,
    WidgetPosition Position,
    WidgetConfig Config);

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

    /// <summary>Widgets configurados no dashboard com posição e config contextual (JSONB).</summary>
    public IReadOnlyList<DashboardWidget> Widgets { get; private set; } = [];

    /// <summary>
    /// Política de partilha granular do dashboard (V3.1).
    /// Substitui o legacy IsShared (bool) com âmbito e permissões finas.
    /// </summary>
    public SharingPolicy SharingPolicy { get; private set; } = SharingPolicy.Private;

    /// <summary>
    /// Variáveis (tokens) do dashboard — permitem parametrizar widgets com contexto dinâmico.
    /// Ex: $service, $team, $env, $timeRange.
    /// </summary>
    public IReadOnlyList<DashboardVariable> Variables { get; private set; } = [];

    /// <summary>Número da revisão atual do dashboard (auto-incrementado em cada Update).</summary>
    public int CurrentRevisionNumber { get; private set; }

    /// <summary>
    /// Retrocompat: indica se o dashboard é partilhado (deriva de SharingPolicy).
    /// Legacy callers que ainda usam IsShared continuam a funcionar.
    /// </summary>
    public bool IsShared => SharingPolicy.IsVisible;

    /// <summary>Estado do ciclo de vida do dashboard (V3.6 — Draft/Published/Deprecated/Archived).</summary>
    public DashboardLifecycleStatus LifecycleStatus { get; private set; } = DashboardLifecycleStatus.Draft;

    /// <summary>Data/hora UTC de deprecação (apenas quando LifecycleStatus = Deprecated).</summary>
    public DateTimeOffset? DeprecatedAt { get; private set; }

    /// <summary>Utilizador que deprecou o dashboard (nullable).</summary>
    public string? DeprecatedByUserId { get; private set; }

    /// <summary>Nota de deprecação explicando o motivo e/ou substituto recomendado.</summary>
    public string? DeprecationNote { get; private set; }

    /// <summary>ID do dashboard que substitui este quando deprecado (nullable).</summary>
    public Guid? SuccessorDashboardId { get; private set; }

    /// <summary>Indica se é um dashboard de sistema criado pelo PlatformAdmin (não editável por outros).</summary>
    public bool IsSystem { get; private init; }

    /// <summary>Identificador opcional da equipa proprietária do dashboard.</summary>
    public string? TeamId { get; private set; }

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

    /// <summary>Número de widgets configurados no dashboard.</summary>
    public int WidgetCount => Widgets.Count;

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
        IReadOnlyList<DashboardWidget> widgets,
        string tenantId,
        string userId,
        DateTimeOffset now,
        string? teamId = null,
        bool isSystem = false)
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
            Widgets = widgets,
            SharingPolicy = SharingPolicy.Private,
            Variables = [],
            CurrentRevisionNumber = 0,
            IsSystem = isSystem,
            TeamId = teamId,
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
            Widgets = Widgets.ToList(),
            Variables = Variables.ToList(),
            SharingPolicy = SharingPolicy.Private,
            CurrentRevisionNumber = 0,
            IsSystem = false,
            TeamId = TeamId,
            TenantId = TenantId,
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Atualiza os dados mutáveis do dashboard incluindo widgets com configuração contextual.
    /// </summary>
    public void Update(
        string name,
        string? description,
        string layout,
        IReadOnlyList<DashboardWidget> widgets,
        string? teamId,
        DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 100, nameof(name));
        Guard.Against.NullOrWhiteSpace(layout, nameof(layout));

        if (description is not null)
            Guard.Against.StringTooLong(description, 500, nameof(description));

        Name = name.Trim();
        Description = description?.Trim();
        Layout = layout.Trim();
        Widgets = widgets;
        TeamId = teamId;
        CurrentRevisionNumber++;
        UpdatedAt = now;
    }

    /// <summary>
    /// Define as variáveis (tokens) do dashboard.
    /// </summary>
    public void SetVariables(IReadOnlyList<DashboardVariable> variables, DateTimeOffset now)
    {
        Guard.Against.Null(variables, nameof(variables));
        Variables = variables;
        UpdatedAt = now;
    }

    /// <summary>
    /// Define a política de partilha granular do dashboard (V3.1).
    /// </summary>
    public void SetSharingPolicy(SharingPolicy policy, DateTimeOffset now)
    {
        Guard.Against.Null(policy, nameof(policy));
        SharingPolicy = policy;
        UpdatedAt = now;
    }

    /// <summary>
    /// Retrocompat: partilha ou retira a partilha via bool (converte para SharingPolicy).
    /// </summary>
    public void SetShared(bool shared, DateTimeOffset now)
        => SetSharingPolicy(SharingPolicy.FromLegacyIsShared(shared), now);

    /// <summary>Publica o dashboard (Draft → Published).</summary>
    public void Publish(DateTimeOffset now)
    {
        if (LifecycleStatus == DashboardLifecycleStatus.Archived)
            throw new InvalidOperationException("Cannot publish an archived dashboard.");
        LifecycleStatus = DashboardLifecycleStatus.Published;
        UpdatedAt = now;
    }

    /// <summary>Depreca o dashboard com nota e substituto opcional (Published → Deprecated).</summary>
    public void Deprecate(string userId, string? note, Guid? successorId, DateTimeOffset now)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        if (LifecycleStatus == DashboardLifecycleStatus.Archived)
            throw new InvalidOperationException("Cannot deprecate an archived dashboard.");
        LifecycleStatus = DashboardLifecycleStatus.Deprecated;
        DeprecatedAt = now;
        DeprecatedByUserId = userId;
        DeprecationNote = note;
        SuccessorDashboardId = successorId;
        UpdatedAt = now;
    }

    /// <summary>Arquiva o dashboard (qualquer estado → Archived).</summary>
    public void Archive(DateTimeOffset now)
    {
        LifecycleStatus = DashboardLifecycleStatus.Archived;
        UpdatedAt = now;
    }

    /// <summary>
    /// Cria um snapshot de revisão do estado atual para persistência no histórico.
    /// Deve ser chamado ANTES de qualquer Update() para capturar o estado anterior,
    /// ou DEPOIS para capturar o estado resultante — por convenção: capturar após update.
    /// </summary>
    public DashboardRevision CreateRevisionSnapshot(
        string widgetsJson,
        string variablesJson,
        string authorUserId,
        DateTimeOffset now,
        string? changeNote = null)
        => DashboardRevision.Create(
            dashboardId: Id,
            revisionNumber: CurrentRevisionNumber,
            name: Name,
            description: Description,
            layout: Layout,
            widgetsJson: widgetsJson,
            variablesJson: variablesJson,
            authorUserId: authorUserId,
            tenantId: TenantId,
            now: now,
            changeNote: changeNote);
}
