using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;

namespace NexTraceOne.Governance.Domain.Entities;

public sealed record DashboardMonitorDefinitionId(Guid Value) : TypedIdBase(Value);

public enum MonitorSeverity { Info = 1, Warning = 2, Critical = 3 }
public enum MonitorConditionOperator { GreaterThan = 1, LessThan = 2, Equals = 3, NotEquals = 4 }
public enum MonitorStatus { Active = 1, Paused = 2, Deleted = 3 }

/// <summary>
/// Monitor de alerta criado a partir de um QueryWidget num dashboard.
/// V3.9 — Advanced NQL, Alerting from Widget &amp; Mobile On-Call Companion.
/// </summary>
public sealed class DashboardMonitorDefinition : Entity<DashboardMonitorDefinitionId>
{
    public Guid DashboardId { get; private init; }
    public string WidgetId { get; private init; } = string.Empty;
    public string TenantId { get; private init; } = string.Empty;
    public string CreatedByUserId { get; private init; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string NqlQuery { get; private set; } = string.Empty;
    public string ConditionField { get; private set; } = string.Empty;
    public MonitorConditionOperator ConditionOperator { get; private set; }
    public decimal ConditionThreshold { get; private set; }
    public int EvaluationWindowMinutes { get; private set; }
    public MonitorSeverity Severity { get; private set; }
    public string NotificationChannelsJson { get; private set; } = "[]";
    public MonitorStatus Status { get; private set; }
    public DateTimeOffset? LastFiredAt { get; private set; }
    public int FiredCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private DashboardMonitorDefinition() { }

    public static DashboardMonitorDefinition Create(
        Guid dashboardId,
        string widgetId,
        string tenantId,
        string userId,
        string name,
        string nqlQuery,
        string conditionField,
        MonitorConditionOperator conditionOperator,
        decimal conditionThreshold,
        int evaluationWindowMinutes,
        MonitorSeverity severity,
        IReadOnlyList<string> notificationChannels,
        DateTimeOffset now)
    {
        return new DashboardMonitorDefinition
        {
            Id = new DashboardMonitorDefinitionId(Guid.NewGuid()),
            DashboardId = dashboardId,
            WidgetId = widgetId,
            TenantId = tenantId,
            CreatedByUserId = userId,
            Name = name,
            NqlQuery = nqlQuery,
            ConditionField = conditionField,
            ConditionOperator = conditionOperator,
            ConditionThreshold = conditionThreshold,
            EvaluationWindowMinutes = evaluationWindowMinutes,
            Severity = severity,
            NotificationChannelsJson = System.Text.Json.JsonSerializer.Serialize(notificationChannels),
            Status = MonitorStatus.Active,
            FiredCount = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void RecordFired(DateTimeOffset now)
    {
        LastFiredAt = now;
        FiredCount++;
        UpdatedAt = now;
    }

    public void Pause(DateTimeOffset now) { Status = MonitorStatus.Paused; UpdatedAt = now; }
    public void Resume(DateTimeOffset now) { Status = MonitorStatus.Active; UpdatedAt = now; }
    public void Delete(DateTimeOffset now) { Status = MonitorStatus.Deleted; UpdatedAt = now; }
}
