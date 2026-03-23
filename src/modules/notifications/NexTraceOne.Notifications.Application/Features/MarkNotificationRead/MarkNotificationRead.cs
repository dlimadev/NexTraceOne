using Ardalis.GuardClauses;
using FluentValidation;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Features.MarkNotificationRead;

/// <summary>
/// Feature: MarkNotificationRead — marca uma notificação como lida pelo destinatário.
/// Valida ownership: apenas o destinatário pode marcar as suas notificações.
/// </summary>
public static class MarkNotificationRead
{
    /// <summary>Comando para marcar uma notificação como lida.</summary>
    public sealed record Command(Guid NotificationId) : ICommand;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.NotificationId).NotEmpty();
        }
    }

    /// <summary>Handler que marca a notificação como lida após validar ownership.</summary>
    public sealed class Handler(
        INotificationStore notificationStore,
        ICurrentUser currentUser) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var userId = Guid.Parse(currentUser.Id);
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

            notification.MarkAsRead();
            await notificationStore.SaveChangesAsync(cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
