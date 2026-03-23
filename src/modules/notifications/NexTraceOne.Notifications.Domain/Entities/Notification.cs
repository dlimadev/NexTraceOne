using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.Events;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Domain.Entities;

/// <summary>
/// Aggregate root da central interna de notificações.
/// Cada instância representa uma notificação entregue a um destinatário específico.
/// A notificação carrega contexto de negócio completo: módulo de origem, entidade,
/// categoria, severidade, deep link e payload adicional.
/// Multi-tenant por design.
/// </summary>
public sealed class Notification : AggregateRoot<NotificationId>
{
    private Notification() { } // EF Core

    private Notification(
        NotificationId id,
        Guid tenantId,
        Guid recipientUserId,
        string eventType,
        NotificationCategory category,
        NotificationSeverity severity,
        string title,
        string message,
        string sourceModule,
        string? sourceEntityType,
        string? sourceEntityId,
        Guid? environmentId,
        string? actionUrl,
        bool requiresAction,
        string? payloadJson,
        DateTimeOffset? expiresAt)
    {
        Id = id;
        TenantId = tenantId;
        RecipientUserId = recipientUserId;
        EventType = eventType;
        Category = category;
        Severity = severity;
        Title = title;
        Message = message;
        SourceModule = sourceModule;
        SourceEntityType = sourceEntityType;
        SourceEntityId = sourceEntityId;
        EnvironmentId = environmentId;
        ActionUrl = actionUrl;
        RequiresAction = requiresAction;
        PayloadJson = payloadJson;
        Status = NotificationStatus.Unread;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;

        RaiseDomainEvent(new NotificationCreatedEvent(
            id.Value,
            recipientUserId,
            category,
            severity,
            sourceModule,
            requiresAction));
    }

    /// <summary>Id do tenant proprietário desta notificação.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Id do utilizador destinatário.</summary>
    public Guid RecipientUserId { get; private set; }

    /// <summary>Tipo do evento de origem (e.g., "IncidentCreated", "ApprovalPending").</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>Categoria funcional da notificação.</summary>
    public NotificationCategory Category { get; private set; }

    /// <summary>Severidade da notificação.</summary>
    public NotificationSeverity Severity { get; private set; }

    /// <summary>Título curto para exibição em lista e push.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Mensagem completa com contexto de negócio.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Módulo de origem que produziu o evento.</summary>
    public string SourceModule { get; private set; } = string.Empty;

    /// <summary>Tipo da entidade de origem para deep linking.</summary>
    public string? SourceEntityType { get; private set; }

    /// <summary>Id da entidade de origem para deep linking.</summary>
    public string? SourceEntityId { get; private set; }

    /// <summary>Id do ambiente onde o evento ocorreu.</summary>
    public Guid? EnvironmentId { get; private set; }

    /// <summary>URL de ação / deep link para navegação direta ao contexto.</summary>
    public string? ActionUrl { get; private set; }

    /// <summary>Se true, a notificação exige ação ou acknowledge do destinatário.</summary>
    public bool RequiresAction { get; private set; }

    /// <summary>Estado atual do ciclo de vida da notificação.</summary>
    public NotificationStatus Status { get; private set; }

    /// <summary>Payload adicional serializado em JSON para templates e contexto rico.</summary>
    public string? PayloadJson { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Data/hora UTC em que foi lida pelo destinatário.</summary>
    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>Data/hora UTC em que foi acknowledged pelo destinatário.</summary>
    public DateTimeOffset? AcknowledgedAt { get; private set; }

    /// <summary>Data/hora UTC em que foi arquivada.</summary>
    public DateTimeOffset? ArchivedAt { get; private set; }

    /// <summary>Data/hora UTC de expiração (após a qual a notificação é descartável).</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// Cria uma nova notificação com contexto de negócio completo.
    /// </summary>
    public static Notification Create(
        Guid tenantId,
        Guid recipientUserId,
        string eventType,
        NotificationCategory category,
        NotificationSeverity severity,
        string title,
        string message,
        string sourceModule,
        string? sourceEntityType = null,
        string? sourceEntityId = null,
        Guid? environmentId = null,
        string? actionUrl = null,
        bool requiresAction = false,
        string? payloadJson = null,
        DateTimeOffset? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceModule);

        return new Notification(
            new NotificationId(Guid.NewGuid()),
            tenantId,
            recipientUserId,
            eventType,
            category,
            severity,
            title,
            message,
            sourceModule,
            sourceEntityType,
            sourceEntityId,
            environmentId,
            actionUrl,
            requiresAction,
            payloadJson,
            expiresAt);
    }

    /// <summary>Marca a notificação como lida.</summary>
    public void MarkAsRead()
    {
        if (Status is NotificationStatus.Unread)
        {
            Status = NotificationStatus.Read;
            ReadAt = DateTimeOffset.UtcNow;

            RaiseDomainEvent(new NotificationReadEvent(
                Id.Value, RecipientUserId, ReadAt.Value));
        }
    }

    /// <summary>Marca a notificação como não lida (reverter leitura).</summary>
    public void MarkAsUnread()
    {
        if (Status is NotificationStatus.Read)
        {
            Status = NotificationStatus.Unread;
            ReadAt = null;
        }
    }

    /// <summary>Regista acknowledge explícito pelo destinatário.</summary>
    public void Acknowledge()
    {
        if (Status is NotificationStatus.Unread or NotificationStatus.Read)
        {
            Status = NotificationStatus.Acknowledged;
            AcknowledgedAt = DateTimeOffset.UtcNow;
            if (ReadAt is null) ReadAt = AcknowledgedAt;
        }
    }

    /// <summary>Arquiva a notificação.</summary>
    public void Archive()
    {
        if (Status is not NotificationStatus.Archived and not NotificationStatus.Dismissed)
        {
            Status = NotificationStatus.Archived;
            ArchivedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>Descarta a notificação sem ação.</summary>
    public void Dismiss()
    {
        if (Status is NotificationStatus.Unread or NotificationStatus.Read)
        {
            Status = NotificationStatus.Dismissed;
        }
    }

    /// <summary>Verifica se a notificação está expirada.</summary>
    public bool IsExpired() => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
}
