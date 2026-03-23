using Ardalis.GuardClauses;
using MediatR;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Application.Features.MarkAllNotificationsRead;

/// <summary>
/// Feature: MarkAllNotificationsRead — marca todas as notificações não lidas
/// do utilizador autenticado como lidas de uma só vez.
/// </summary>
public static class MarkAllNotificationsRead
{
    /// <summary>Comando para marcar todas as notificações como lidas.</summary>
    public sealed record Command() : ICommand;

    /// <summary>Handler que marca todas as notificações não lidas do utilizador como lidas.</summary>
    public sealed class Handler(
        INotificationStore notificationStore,
        ICurrentUser currentUser) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var userId = Guid.Parse(currentUser.Id);
            await notificationStore.MarkAllAsReadAsync(userId, cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
