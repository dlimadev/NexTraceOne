using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Notifications.Application.Abstractions;
using NexTraceOne.Notifications.Domain.Enums;

namespace NexTraceOne.Notifications.Application.Features.ListNotifications;

/// <summary>
/// Feature: ListNotifications — lista notificações do utilizador autenticado
/// com filtros opcionais de status, categoria e severidade mínima.
/// </summary>
public static class ListNotifications
{
    /// <summary>Query de listagem de notificações com filtros e paginação.</summary>
    public sealed record Query(
        string? Status,
        string? Category,
        string? Severity,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>, IPagedQuery;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            RuleFor(x => x.Status)
                .Must(s => s is null || Enum.TryParse<NotificationStatus>(s, ignoreCase: true, out _))
                .WithMessage("Invalid notification status.");

            RuleFor(x => x.Category)
                .Must(c => c is null || Enum.TryParse<NotificationCategory>(c, ignoreCase: true, out _))
                .WithMessage("Invalid notification category.");

            RuleFor(x => x.Severity)
                .Must(s => s is null || Enum.TryParse<NotificationSeverity>(s, ignoreCase: true, out _))
                .WithMessage("Invalid notification severity.");
        }
    }

    /// <summary>Handler que lista notificações do utilizador autenticado.</summary>
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

            var status = request.Status is not null
                ? Enum.Parse<NotificationStatus>(request.Status, ignoreCase: true)
                : (NotificationStatus?)null;

            var category = request.Category is not null
                ? Enum.Parse<NotificationCategory>(request.Category, ignoreCase: true)
                : (NotificationCategory?)null;

            var severity = request.Severity is not null
                ? Enum.Parse<NotificationSeverity>(request.Severity, ignoreCase: true)
                : (NotificationSeverity?)null;

            var skip = (request.Page - 1) * request.PageSize;
            var take = request.PageSize + 1; // fetch one extra to determine HasMore

            var items = await notificationStore.ListAsync(
                userId, status, category, severity, skip, take, cancellationToken);

            var hasMore = items.Count > request.PageSize;

            var dtos = items
                .Take(request.PageSize)
                .Select(n => new NotificationDto(
                    n.Id.Value,
                    n.Title,
                    n.Message,
                    n.Category.ToString(),
                    n.Severity.ToString(),
                    n.Status.ToString(),
                    n.SourceModule,
                    n.SourceEntityType,
                    n.SourceEntityId,
                    n.ActionUrl,
                    n.RequiresAction,
                    n.CreatedAt,
                    n.ReadAt))
                .ToList();

            return new Response(dtos, hasMore);
        }
    }

    /// <summary>Resposta da listagem de notificações.</summary>
    public sealed record Response(IReadOnlyList<NotificationDto> Items, bool HasMore);

    /// <summary>DTO de projeção de uma notificação para a API.</summary>
    public sealed record NotificationDto(
        Guid Id,
        string Title,
        string Message,
        string Category,
        string Severity,
        string Status,
        string SourceModule,
        string? SourceEntityType,
        string? SourceEntityId,
        string? ActionUrl,
        bool RequiresAction,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ReadAt);
}
