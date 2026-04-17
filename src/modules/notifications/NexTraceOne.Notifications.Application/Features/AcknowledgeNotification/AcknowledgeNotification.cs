using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Features.AcknowledgeNotification;

/// <summary>
/// Feature: AcknowledgeNotification — regista acknowledge explícito de uma notificação pelo destinatário.
/// Aplicável a notificações com RequiresAction=true que precisam de confirmação activa.
/// Transição de estado: Unread|Read → Acknowledged.
/// </summary>
public static class AcknowledgeNotification
{
    /// <summary>Comando para registar acknowledge de uma notificação.</summary>
    public sealed record Command(
        Guid NotificationId,
        string? Comment = null) : ICommand;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.NotificationId).NotEmpty();
            RuleFor(x => x.Comment).MaximumLength(1000).When(x => x.Comment is not null);
        }
    }

    /// <summary>Handler que regista acknowledge da notificação após validar ownership.</summary>
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

            if (notification.Status is NotificationStatus.Archived or NotificationStatus.Dismissed)
                return Error.Conflict(
                    "Notification.AlreadyClosed",
                    "Notification {0} is already archived or dismissed.",
                    request.NotificationId);

            notification.Acknowledge(userId, request.Comment);
            await notificationStore.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
