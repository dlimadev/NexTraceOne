using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using ListNotificationsFeature = NexTraceOne.Notifications.Application.Features.ListNotifications.ListNotifications;
using GetUnreadCountFeature = NexTraceOne.Notifications.Application.Features.GetUnreadCount.GetUnreadCount;
using MarkNotificationReadFeature = NexTraceOne.Notifications.Application.Features.MarkNotificationRead.MarkNotificationRead;
using MarkNotificationUnreadFeature = NexTraceOne.Notifications.Application.Features.MarkNotificationUnread.MarkNotificationUnread;
using MarkAllNotificationsReadFeature = NexTraceOne.Notifications.Application.Features.MarkAllNotificationsRead.MarkAllNotificationsRead;
using GetPreferencesFeature = NexTraceOne.Notifications.Application.Features.GetPreferences.GetPreferences;
using UpdatePreferenceFeature = NexTraceOne.Notifications.Application.Features.UpdatePreference.UpdatePreference;
using GetDeliveryHistoryFeature = NexTraceOne.Notifications.Application.Features.GetDeliveryHistory.GetDeliveryHistory;
using GetDeliveryStatusFeature = NexTraceOne.Notifications.Application.Features.GetDeliveryStatus.GetDeliveryStatus;
using GetNotificationTrailFeature = NexTraceOne.Notifications.Application.Features.GetNotificationTrail.GetNotificationTrail;
using AcknowledgeNotificationFeature = NexTraceOne.Notifications.Application.Features.AcknowledgeNotification.AcknowledgeNotification;
using ArchiveNotificationFeature = NexTraceOne.Notifications.Application.Features.ArchiveNotification.ArchiveNotification;
using DismissNotificationFeature = NexTraceOne.Notifications.Application.Features.DismissNotification.DismissNotification;
using SnoozeNotificationFeature = NexTraceOne.Notifications.Application.Features.SnoozeNotification.SnoozeNotification;

namespace NexTraceOne.Notifications.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Notifications.
/// </summary>
public sealed class NotificationCenterEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications");

        group.MapGet("/", async (
            string? status,
            string? category,
            string? severity,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(
                new ListNotificationsFeature.Query(status, category, severity, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:inbox:read");

        group.MapGet("/unread-count", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetUnreadCountFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:inbox:read");

        group.MapPost("/{id:guid}/read", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new MarkNotificationReadFeature.Command(id), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:inbox:write");

        group.MapPost("/{id:guid}/unread", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new MarkNotificationUnreadFeature.Command(id), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:inbox:write");

        group.MapPost("/mark-all-read", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new MarkAllNotificationsReadFeature.Command(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:inbox:write");

        group.MapGet("/preferences", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPreferencesFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:preferences:read");

        group.MapPut("/preferences", async (
            UpdatePreferenceFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:preferences:write");

        // ── Lifecycle: Acknowledge, Archive, Dismiss, Snooze ─────────────────

        group.MapPost("/{id:guid}/acknowledge", async (
            Guid id,
            AcknowledgeNotificationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var effectiveCommand = command with { NotificationId = id };
            var result = await sender.Send(effectiveCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:inbox:write");

        group.MapPost("/{id:guid}/archive", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ArchiveNotificationFeature.Command(id), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:inbox:write");

        group.MapPost("/{id:guid}/dismiss", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DismissNotificationFeature.Command(id), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:inbox:write");

        group.MapPost("/{id:guid}/snooze", async (
            Guid id,
            SnoozeNotificationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var effectiveCommand = command with { NotificationId = id };
            var result = await sender.Send(effectiveCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:inbox:write");

        // ── P7.2: Delivery History & Status ───────────────────────────────────

        group.MapGet("/{id:guid}/delivery-history", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetDeliveryHistoryFeature.Query(id), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:delivery:read");

        group.MapGet("/{id:guid}/delivery-status", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetDeliveryStatusFeature.Query(id), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:delivery:read");

        // ── P7.3: Notification Audit Trail ────────────────────────────────────

        group.MapGet("/{id:guid}/trail", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetNotificationTrailFeature.Query(id), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:delivery:read");
    }
}
