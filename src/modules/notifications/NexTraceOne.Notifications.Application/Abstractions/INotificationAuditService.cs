namespace NexTraceOne.Notifications.Application.Abstractions;

/// <summary>
/// Serviço de auditoria para notificações críticas e ações relevantes do utilizador.
/// Phase 7 — integra com o módulo de auditoria existente do NexTraceOne.
/// </summary>
public interface INotificationAuditService
{
    /// <summary>
    /// Regista evento de auditoria para uma acção na plataforma de notificações.
    /// Eventos auditáveis: geração/entrega/falha de notificações críticas,
    /// acknowledge, snooze, escalation, criação de incidente, mudança de preferências.
    /// </summary>
    Task RecordAsync(
        NotificationAuditEntry entry,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Entrada de auditoria para a plataforma de notificações.
/// </summary>
public sealed record NotificationAuditEntry
{
    /// <summary>Id do tenant.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Tipo de acção auditada.</summary>
    public required string ActionType { get; init; }

    /// <summary>Id do recurso (notificação, preferência, etc.).</summary>
    public required string ResourceId { get; init; }

    /// <summary>Tipo do recurso.</summary>
    public required string ResourceType { get; init; }

    /// <summary>Id do utilizador que executou a acção (null para acções automáticas).</summary>
    public Guid? PerformedBy { get; init; }

    /// <summary>Descrição da acção.</summary>
    public string? Description { get; init; }

    /// <summary>Payload adicional em JSON.</summary>
    public string? PayloadJson { get; init; }

    /// <summary>Data/hora da ocorrência.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Tipos de acção auditável na plataforma de notificações.
/// </summary>
public static class NotificationAuditActions
{
    public const string CriticalNotificationGenerated = "notification.critical.generated";
    public const string CriticalNotificationDelivered = "notification.critical.delivered";
    public const string CriticalNotificationFailed = "notification.critical.failed";
    public const string NotificationAcknowledged = "notification.acknowledged";
    public const string NotificationSnoozed = "notification.snoozed";
    public const string NotificationEscalated = "notification.escalated";
    public const string IncidentCreatedFromNotification = "notification.incident.created";
    public const string PreferencesChanged = "notification.preferences.changed";
    public const string NotificationSuppressed = "notification.suppressed";
}
