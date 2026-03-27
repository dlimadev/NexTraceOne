using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.ExternalDelivery;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Features.GetDeliveryHistory;

/// <summary>
/// Feature: GetDeliveryHistory — retorna o histórico de tentativas de entrega de uma notificação.
/// Permite auditar quantas tentativas houve, por que falhou e quando foi entregue.
/// </summary>
public static class GetDeliveryHistory
{
    /// <summary>Query que solicita o histórico de deliveries de uma notificação.</summary>
    public sealed record Query(Guid NotificationId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.NotificationId).NotEmpty();
        }
    }

    /// <summary>DTO de um registo de delivery.</summary>
    public sealed record DeliveryEntryDto(
        Guid Id,
        string Channel,
        string Status,
        string? RecipientAddress,
        int RetryCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastAttemptAt,
        DateTimeOffset? DeliveredAt,
        DateTimeOffset? FailedAt,
        DateTimeOffset? NextRetryAt,
        string? ErrorMessage);

    /// <summary>Resposta com a lista de deliveries para a notificação solicitada.</summary>
    public sealed record Response(
        Guid NotificationId,
        IReadOnlyList<DeliveryEntryDto> Deliveries,
        int TotalAttempts,
        bool HasSuccessfulDelivery);

    /// <summary>Handler que retorna o histórico de deliveries de uma notificação.</summary>
    public sealed class Handler(
        INotificationDeliveryStore deliveryStore,
        ICurrentUser currentUser) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            var notificationId = new NotificationId(query.NotificationId);
            var deliveries = await deliveryStore.ListByNotificationIdAsync(notificationId, cancellationToken);

            var dtos = deliveries
                .Select(d => new DeliveryEntryDto(
                    Id: d.Id.Value,
                    Channel: d.Channel.ToString(),
                    Status: d.Status.ToString(),
                    RecipientAddress: d.RecipientAddress,
                    RetryCount: d.RetryCount,
                    CreatedAt: d.CreatedAt,
                    LastAttemptAt: d.LastAttemptAt,
                    DeliveredAt: d.DeliveredAt,
                    FailedAt: d.FailedAt,
                    NextRetryAt: d.NextRetryAt,
                    ErrorMessage: d.ErrorMessage))
                .ToList();

            var response = new Response(
                NotificationId: query.NotificationId,
                Deliveries: dtos,
                TotalAttempts: deliveries.Sum(d => d.RetryCount),
                HasSuccessfulDelivery: deliveries.Any(d => d.Status == DeliveryStatus.Delivered));

            return response;
        }
    }
}
