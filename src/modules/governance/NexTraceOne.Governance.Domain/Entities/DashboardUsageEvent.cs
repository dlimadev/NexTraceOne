using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

/// <summary>Identificador fortemente tipado para DashboardUsageEvent.</summary>
public sealed record DashboardUsageEventId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Registo de um evento de uso de dashboard (V3.6 — Usage Analytics).
/// Alimenta métricas de curadoria: dashboards mais vistos, por quem, com que frequência.
/// </summary>
public sealed class DashboardUsageEvent : Entity<DashboardUsageEventId>
{
    /// <summary>Dashboard acedido.</summary>
    public Guid DashboardId { get; private init; }

    /// <summary>Tenant do utilizador.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Utilizador que acedeu (nullable para acessos públicos anónimos).</summary>
    public string? UserId { get; private init; }

    /// <summary>Persona do utilizador no momento do acesso.</summary>
    public string? Persona { get; private init; }

    /// <summary>Tipo de evento: "view", "export", "embed", "share", "snapshot".</summary>
    public string EventType { get; private init; } = "view";

    /// <summary>Duração da sessão de visualização em segundos (nullable se desconhecida).</summary>
    public int? DurationSeconds { get; private init; }

    /// <summary>Data/hora UTC do evento.</summary>
    public DateTimeOffset OccurredAt { get; private init; }

    private DashboardUsageEvent() { }

    public static DashboardUsageEvent Record(
        Guid dashboardId,
        string tenantId,
        string? userId,
        string? persona,
        string eventType,
        DateTimeOffset now,
        int? durationSeconds = null)
    {
        Guard.Against.Default(dashboardId, nameof(dashboardId));
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));

        return new DashboardUsageEvent
        {
            Id = new DashboardUsageEventId(Guid.NewGuid()),
            DashboardId = dashboardId,
            TenantId = tenantId,
            UserId = userId,
            Persona = persona,
            EventType = eventType,
            DurationSeconds = durationSeconds,
            OccurredAt = now
        };
    }
}
