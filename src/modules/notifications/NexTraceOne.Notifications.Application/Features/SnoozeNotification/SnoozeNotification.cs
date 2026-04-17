using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Features.SnoozeNotification;

/// <summary>
/// Feature: SnoozeNotification — adia o alerta de uma notificação até uma data/hora futura.
/// Durante o período de snooze, a notificação não aparece como activa na inbox.
/// </summary>
public static class SnoozeNotification
{
    /// <summary>Comando para adiar uma notificação.</summary>
    public sealed record Command(
        Guid NotificationId,
        DateTimeOffset SnoozedUntil) : ICommand;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.NotificationId).NotEmpty();
            RuleFor(x => x.SnoozedUntil)
                .GreaterThan(DateTimeOffset.UtcNow)
                .WithMessage("SnoozedUntil must be a future date/time.");
        }
    }

    /// <summary>Handler que adia a notificação após validar ownership e estado.</summary>
    public sealed class Handler(
        INotificationStore notificationStore,
        ICurrentUser currentUser) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!Guid.TryParse(currentUser.Id, out var userId))
                return Error.Unauthorized(
                    "Notification.InvalidUserId",
                    "Current user identifier is not a valid GUID.");

            var notification = await notificationStore.GetByIdAsync(
                new NotificationId(request.NotificationId), cancellationToken);

            if (notification is null)
                return Error.NotFound(
                    "Notification.NotFound",
                    "Notification {0} was not found.",
                    request.NotificationId);

            if (notification.RecipientUserId != userId)
                return Error.Forbidden(
                    "Notification.Forbidden",
                    "You do not have access to this notification.");

            if (notification.Status is NotificationStatus.Dismissed or NotificationStatus.Archived)
                return Error.Conflict(
                    "Notification.AlreadyClosed",
                    "Notification {0} cannot be snoozed from its current state.",
                    request.NotificationId);

            if (request.SnoozedUntil <= DateTimeOffset.UtcNow)
                return Error.Validation(
                    "Notification.InvalidSnoozeDate",
                    "SnoozedUntil must be a future date/time.");

            notification.Snooze(request.SnoozedUntil, userId);
            await notificationStore.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
