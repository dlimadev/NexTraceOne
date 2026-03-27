using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Features.GetDeliveryStatus;

/// <summary>
/// Feature: GetDeliveryStatus — retorna o estado agregado de entrega de uma notificação.
/// Responde a: foi entregue? Em que canais? Quantas tentativas? Há retries pendentes?
/// </summary>
public static class GetDeliveryStatus
{
    /// <summary>Query que solicita o status de entrega de uma notificação.</summary>
    public sealed record Query(Guid NotificationId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.NotificationId).NotEmpty();
        }
    }

    /// <summary>Sumário de entrega por canal.</summary>
    public sealed record ChannelStatusDto(
        string Channel,
        string Status,
        int RetryCount,
        DateTimeOffset? LastAttemptAt,
        DateTimeOffset? DeliveredAt,
        DateTimeOffset? NextRetryAt,
        string? LastError);

    /// <summary>Resposta com o estado agregado de entrega.</summary>
    public sealed record Response(
        Guid NotificationId,
        bool IsDeliveredToAnyChannel,
        bool HasPendingRetry,
        bool HasPermanentFailure,
        int TotalChannelAttempts,
        IReadOnlyList<ChannelStatusDto> ChannelStatuses);

    /// <summary>Handler que agrega o status de entrega por canal de uma notificação.</summary>
    public sealed class Handler(
        INotificationDeliveryStore deliveryStore) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            var notificationId = new NotificationId(query.NotificationId);
            var deliveries = await deliveryStore.ListByNotificationIdAsync(notificationId, cancellationToken);

            var channelStatuses = deliveries
                .Select(d => new ChannelStatusDto(
                    Channel: d.Channel.ToString(),
                    Status: d.Status.ToString(),
                    RetryCount: d.RetryCount,
                    LastAttemptAt: d.LastAttemptAt,
                    DeliveredAt: d.DeliveredAt,
                    NextRetryAt: d.NextRetryAt,
                    LastError: d.ErrorMessage))
                .ToList();

            var response = new Response(
                NotificationId: query.NotificationId,
                IsDeliveredToAnyChannel: deliveries.Any(d => d.Status == DeliveryStatus.Delivered),
                HasPendingRetry: deliveries.Any(d => d.Status == DeliveryStatus.RetryScheduled),
                HasPermanentFailure: deliveries.Any(d => d.Status == DeliveryStatus.Failed),
                TotalChannelAttempts: deliveries.Sum(d => d.RetryCount),
                ChannelStatuses: channelStatuses);

            return response;
        }
    }
}
