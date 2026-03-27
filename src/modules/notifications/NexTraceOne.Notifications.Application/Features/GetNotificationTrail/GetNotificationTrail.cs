using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Features.GetNotificationTrail;

/// <summary>
/// Feature: GetNotificationTrail — retorna a trilha de correlação completa de uma notificação.
/// Permite rastrear: evento de origem → notificação criada → tentativas de entrega por canal.
/// P7.3: implementado para fechar a rastreabilidade auditável do módulo Notifications.
/// </summary>
public static class GetNotificationTrail
{
    /// <summary>Query para obter a trilha de correlação de uma notificação.</summary>
    public sealed record Query(Guid NotificationId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.NotificationId).NotEmpty();
        }
    }

    /// <summary>DTO da notificação com contexto de correlação.</summary>
    public sealed record NotificationCorrelationDto(
        Guid NotificationId,
        string EventType,
        string SourceModule,
        string? SourceEntityType,
        string? SourceEntityId,
        string? SourceEventId,
        string Category,
        string Severity,
        string Status,
        Guid RecipientUserId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReadAt,
        bool RequiresAction);

    /// <summary>DTO de uma tentativa de entrega na trilha.</summary>
    public sealed record DeliveryTrailEntryDto(
        Guid DeliveryId,
        string Channel,
        string Status,
        int RetryCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastAttemptAt,
        DateTimeOffset? DeliveredAt,
        DateTimeOffset? FailedAt,
        DateTimeOffset? NextRetryAt,
        string? ErrorMessage);

    /// <summary>Resposta com a trilha de correlação completa.</summary>
    public sealed record Response(
        Guid NotificationId,
        NotificationCorrelationDto Notification,
        IReadOnlyList<DeliveryTrailEntryDto> Deliveries,
        int TotalDeliveryAttempts,
        bool IsDeliveredToAnyChannel,
        bool HasPendingRetry,
        bool HasPermanentFailure);

    public sealed class Handler(
        INotificationStore notificationStore,
        INotificationDeliveryStore deliveryStore) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var notificationId = new NotificationId(request.NotificationId);

            var notification = await notificationStore.GetByIdAsync(notificationId, cancellationToken);
            if (notification is null)
                return Error.NotFound(
                    "Notification.NotFound",
                    "Notification {0} was not found.",
                    request.NotificationId);

            var deliveries = await deliveryStore.ListByNotificationIdAsync(notificationId, cancellationToken);

            var notificationDto = new NotificationCorrelationDto(
                notification.Id.Value,
                notification.EventType,
                notification.SourceModule,
                notification.SourceEntityType,
                notification.SourceEntityId,
                notification.SourceEventId,
                notification.Category.ToString(),
                notification.Severity.ToString(),
                notification.Status.ToString(),
                notification.RecipientUserId,
                notification.CreatedAt,
                notification.ReadAt,
                notification.RequiresAction);

            var deliveryDtos = deliveries
                .Select(d => new DeliveryTrailEntryDto(
                    d.Id.Value,
                    d.Channel.ToString(),
                    d.Status.ToString(),
                    d.RetryCount,
                    d.CreatedAt,
                    d.LastAttemptAt,
                    d.DeliveredAt,
                    d.FailedAt,
                    d.NextRetryAt,
                    d.ErrorMessage))
                .ToList();

            var totalAttempts = deliveries.Sum(d => d.RetryCount);
            var isDelivered = deliveries.Any(d => d.DeliveredAt.HasValue);
            var hasPendingRetry = deliveries.Any(d => d.NextRetryAt.HasValue);
            var hasPermanentFailure = deliveries.Any(d =>
                d.FailedAt.HasValue && !d.NextRetryAt.HasValue && !d.DeliveredAt.HasValue);

            return new Response(
                notification.Id.Value,
                notificationDto,
                deliveryDtos,
                totalAttempts,
                isDelivered,
                hasPendingRetry,
                hasPermanentFailure);
        }
    }
}
