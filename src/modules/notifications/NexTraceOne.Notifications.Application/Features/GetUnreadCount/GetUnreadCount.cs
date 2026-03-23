using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;

namespace NexTraceOne.Notifications.Application.Features.GetUnreadCount;

/// <summary>
/// Feature: GetUnreadCount — obtém o total de notificações não lidas do utilizador autenticado.
/// </summary>
public static class GetUnreadCount
{
    /// <summary>Query de contagem de notificações não lidas.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que conta notificações não lidas do utilizador autenticado.</summary>
    public sealed class Handler(
        INotificationStore notificationStore,
        ICurrentUser currentUser) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!Guid.TryParse(currentUser.Id, out var userId))
                return Error.Unauthorized(
                    "Notification.InvalidUserId",
                    "Current user identifier is not a valid GUID.");
            var count = await notificationStore.CountUnreadAsync(userId, cancellationToken);

            return new Response(count);
        }
    }

    /// <summary>Resposta com contagem de notificações não lidas.</summary>
    public sealed record Response(int UnreadCount);
}
