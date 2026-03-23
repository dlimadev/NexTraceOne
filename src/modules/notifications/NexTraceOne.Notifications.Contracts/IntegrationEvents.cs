using NexTraceOne.BuildingBlocks.Core.Events;

namespace NexTraceOne.Notifications.Contracts;

/// <summary>
/// Eventos de integração publicados pelo módulo Notifications.
/// Outros módulos podem reagir a estes eventos para enriquecer ou auditar notificações.
/// </summary>
public static class IntegrationEvents
{
    /// <summary>
    /// Publicado quando uma notificação é criada e persistida na central interna.
    /// Consumidores típicos: Audit, OperationalIntelligence.
    /// </summary>
    public sealed record NotificationCreatedIntegrationEvent(
        Guid NotificationId,
        Guid RecipientUserId,
        string Category,
        string Severity,
        string SourceModule,
        string? SourceEntityId,
        DateTimeOffset CreatedAt) : IntegrationEventBase("Notifications");

    /// <summary>
    /// Publicado quando uma notificação é entregue com sucesso por um canal externo.
    /// Consumidores típicos: Audit.
    /// </summary>
    public sealed record NotificationDeliveredIntegrationEvent(
        Guid NotificationId,
        Guid DeliveryId,
        string Channel,
        DateTimeOffset DeliveredAt) : IntegrationEventBase("Notifications");

    /// <summary>
    /// Publicado quando a entrega de uma notificação falha num canal externo.
    /// Consumidores típicos: OperationalIntelligence, Audit.
    /// </summary>
    public sealed record NotificationDeliveryFailedIntegrationEvent(
        Guid NotificationId,
        Guid DeliveryId,
        string Channel,
        string? ErrorMessage,
        DateTimeOffset FailedAt) : IntegrationEventBase("Notifications");
}
