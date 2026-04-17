using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.StronglyTypedIds;

namespace NexTraceOne.Notifications.Application.Features.GetNotificationById;

/// <summary>
/// Feature: GetNotificationById — retorna o detalhe completo de uma notificação por ID.
/// Usado pela página de detalhe da notificação para mostrar título, mensagem,
/// estado de entrega, trail de auditoria e ações disponíveis.
/// </summary>
public static class GetNotificationById
{
    /// <summary>Query de detalhe de uma notificação individual.</summary>
    public sealed record Query(Guid NotificationId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.NotificationId).NotEmpty();
        }
    }

    /// <summary>DTO completo de uma notificação para a vista de detalhe.</summary>
    public sealed record NotificationDetailDto(
        Guid Id,
        string Title,
        string Message,
        string Category,
        string Severity,
        string Status,
        string EventType,
        string SourceModule,
        string? SourceEntityType,
        string? SourceEntityId,
        string? SourceEventId,
        string? ActionUrl,
        bool RequiresAction,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReadAt,
        DateTimeOffset? AcknowledgedAt,
        DateTimeOffset? ArchivedAt,
        DateTimeOffset? SnoozedUntil,
        bool IsEscalated,
        int OccurrenceCount,
        Guid? EnvironmentId);

    /// <summary>Resposta do detalhe de uma notificação.</summary>
    public sealed record Response(NotificationDetailDto Notification);

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

            var id = new NotificationId(request.NotificationId);
            var notification = await notificationStore.GetByIdAsync(id, cancellationToken);

            if (notification is null)
                return Error.NotFound(
                    "Notification.NotFound",
                    "Notification {0} was not found.",
                    request.NotificationId);

            // Só o destinatário pode ver o detalhe da sua notificação.
            if (notification.RecipientUserId != userId)
                return Error.Forbidden(
                    "Notification.AccessDenied",
                    "You do not have access to this notification.");

            var dto = new NotificationDetailDto(
                notification.Id.Value,
                notification.Title,
                notification.Message,
                notification.Category.ToString(),
                notification.Severity.ToString(),
                notification.Status.ToString(),
                notification.EventType,
                notification.SourceModule,
                notification.SourceEntityType,
                notification.SourceEntityId,
                notification.SourceEventId,
                notification.ActionUrl,
                notification.RequiresAction,
                notification.CreatedAt,
                notification.ReadAt,
                notification.AcknowledgedAt,
                notification.ArchivedAt,
                notification.SnoozedUntil,
                notification.IsEscalated,
                notification.OccurrenceCount,
                notification.EnvironmentId);

            return new Response(dto);
        }
    }
}
