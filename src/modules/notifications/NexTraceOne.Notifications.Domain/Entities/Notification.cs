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

    /// <summary>Id do utilizador que fez acknowledge.</summary>
    public Guid? AcknowledgedBy { get; private set; }

    /// <summary>Comentário opcional do acknowledge.</summary>
    public string? AcknowledgeComment { get; private set; }

    /// <summary>Data/hora UTC em que foi arquivada.</summary>
    public DateTimeOffset? ArchivedAt { get; private set; }

    /// <summary>Data/hora UTC de expiração (após a qual a notificação é descartável).</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    // ── Phase 6: Intelligence & Automation ──

    /// <summary>Chave de correlação para agrupar notificações relacionadas.</summary>
    public string? CorrelationKey { get; private set; }

    /// <summary>Id do grupo ao qual esta notificação pertence.</summary>
    public Guid? GroupId { get; private set; }

    /// <summary>Contagem de ocorrências (para deduplicação com incremento).</summary>
    public int OccurrenceCount { get; private set; } = 1;

    /// <summary>Data/hora UTC da última ocorrência (actualizada por deduplicação).</summary>
    public DateTimeOffset? LastOccurrenceAt { get; private set; }

    /// <summary>Data/hora UTC até à qual a notificação está snoozed.</summary>
    public DateTimeOffset? SnoozedUntil { get; private set; }

    /// <summary>Id do utilizador que fez snooze.</summary>
    public Guid? SnoozedBy { get; private set; }

    /// <summary>Indica se a notificação foi escalada.</summary>
    public bool IsEscalated { get; private set; }

    /// <summary>Data/hora UTC da escalação.</summary>
    public DateTimeOffset? EscalatedAt { get; private set; }

    /// <summary>Id do incidente correlacionado (se existir).</summary>
    public Guid? CorrelatedIncidentId { get; private set; }

    /// <summary>Indica se a notificação foi suprimida por regra.</summary>
    public bool IsSuppressed { get; private set; }

    /// <summary>Razão da supressão (para auditabilidade).</summary>
    public string? SuppressionReason { get; private set; }

    /// <summary>Token de concorrência otimista (PostgreSQL xmin).</summary>
    public uint RowVersion { get; set; }

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
    public void Acknowledge(Guid? userId = null, string? comment = null)
    {
        if (Status is NotificationStatus.Unread or NotificationStatus.Read)
        {
            Status = NotificationStatus.Acknowledged;
            AcknowledgedAt = DateTimeOffset.UtcNow;
            AcknowledgedBy = userId;
            AcknowledgeComment = comment;
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

    // ── Phase 6: Intelligence & Automation methods ──

    /// <summary>Define a chave de correlação e/ou grupo.</summary>
    public void SetCorrelation(string? correlationKey, Guid? groupId = null)
    {
        CorrelationKey = correlationKey;
        GroupId = groupId;
    }

    /// <summary>Incrementa o contador de ocorrências (deduplicação com merge).</summary>
    public void IncrementOccurrence()
    {
        OccurrenceCount++;
        LastOccurrenceAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Adia a notificação até a data especificada.</summary>
    public void Snooze(DateTimeOffset until, Guid snoozedBy)
    {
        if (Status is NotificationStatus.Dismissed or NotificationStatus.Archived)
            return;

        SnoozedUntil = until;
        SnoozedBy = snoozedBy;
    }

    /// <summary>Remove o snooze (reactivação manual ou por expiração).</summary>
    public void Unsnooze()
    {
        SnoozedUntil = null;
        SnoozedBy = null;
    }

    /// <summary>Verifica se a notificação está actualmente snoozed.</summary>
    public bool IsSnoozed() => SnoozedUntil.HasValue && DateTimeOffset.UtcNow < SnoozedUntil.Value;

    /// <summary>Marca a notificação como escalada.</summary>
    public void MarkAsEscalated()
    {
        if (!IsEscalated)
        {
            IsEscalated = true;
            EscalatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>Correlaciona a notificação com um incidente.</summary>
    public void CorrelateWithIncident(Guid incidentId)
    {
        CorrelatedIncidentId = incidentId;
    }

    /// <summary>Marca a notificação como suprimida.</summary>
    public void Suppress(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        IsSuppressed = true;
        SuppressionReason = reason;
    }

    /// <summary>Verifica se a notificação está expirada.</summary>
    public bool IsExpired() => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
}
