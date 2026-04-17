using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Features.ArchiveNotification;

/// <summary>
/// Feature: ArchiveNotification — arquiva uma notificação pelo destinatário.
/// Notificações arquivadas são preservadas no histórico mas removidas da inbox activa.
/// Transição de estado: Unread|Read|Acknowledged → Archived.
/// </summary>
public static class ArchiveNotification
{
    /// <summary>Comando para arquivar uma notificação.</summary>
    public sealed record Command(Guid NotificationId) : ICommand;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.NotificationId).NotEmpty();
        }
    }

    /// <summary>Handler que arquiva a notificação após validar ownership.</summary>
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

            if (notification.Status is NotificationStatus.Dismissed)
                return Error.Conflict(
                    "Notification.AlreadyDismissed",
                    "Notification {0} is already dismissed.",
                    request.NotificationId);

            notification.Archive();
            await notificationStore.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
