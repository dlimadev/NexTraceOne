using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Notifications.Application.Abstractions;

using ListTemplatesFeature = NexTraceOne.Notifications.Application.Features.ListNotificationTemplates.ListNotificationTemplates;
using UpsertTemplateFeature = NexTraceOne.Notifications.Application.Features.UpsertNotificationTemplate.UpsertNotificationTemplate;
using ListChannelsFeature = NexTraceOne.Notifications.Application.Features.ListDeliveryChannels.ListDeliveryChannels;
using UpsertChannelFeature = NexTraceOne.Notifications.Application.Features.UpsertDeliveryChannel.UpsertDeliveryChannel;
using GetSmtpFeature = NexTraceOne.Notifications.Application.Features.GetSmtpConfiguration.GetSmtpConfiguration;
using UpsertSmtpFeature = NexTraceOne.Notifications.Application.Features.UpsertSmtpConfiguration.UpsertSmtpConfiguration;

namespace NexTraceOne.Notifications.API.Endpoints;

/// <summary>
/// Registra endpoints de configuração do módulo Notifications:
/// templates persistidos, canais de entrega e configuração SMTP.
/// </summary>
public sealed class NotificationConfigurationEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications/configuration");

        // ── Templates ─────────────────────────────────────────────────────────

        group.MapGet("/templates", async (
            string? eventType,
            string? channel,
            bool? isActive,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListTemplatesFeature.Query(eventType, channel, isActive),
                cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:configuration:read");

        group.MapPut("/templates", async (
            UpsertTemplateFeature.Command command,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:configuration:write");

        // ── Channels ──────────────────────────────────────────────────────────

        group.MapGet("/channels", async (
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListChannelsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:configuration:read");

        group.MapPut("/channels", async (
            UpsertChannelFeature.Command command,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:configuration:write");

        // ── SMTP ──────────────────────────────────────────────────────────────

        group.MapGet("/smtp", async (
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetSmtpFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:configuration:read");

        group.MapPut("/smtp", async (
            UpsertSmtpFeature.Command command,
            [FromServices] ISender sender,
            [FromServices] IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:configuration:write");

        // ── Analytics ─────────────────────────────────────────────────────────

        group.MapGet("/analytics", async (
            int? days,
            [FromServices] INotificationMetricsService metricsService,
            [FromServices] ICurrentTenant currentTenant,
            CancellationToken cancellationToken) =>
        {
            if (currentTenant.Id == Guid.Empty || !currentTenant.IsActive)
            {
                return Results.BadRequest(new { error = "Tenant context is required." });
            }

            var lookbackDays = days.GetValueOrDefault(30);
            if (lookbackDays is < 1 or > 90)
            {
                return Results.BadRequest(new { error = "Days must be between 1 and 90." });
            }

            var until = DateTimeOffset.UtcNow;
            var from = until.AddDays(-lookbackDays);

            var platform = await metricsService.GetPlatformMetricsAsync(
                currentTenant.Id,
                from,
                until,
                cancellationToken);

            var interaction = await metricsService.GetInteractionMetricsAsync(
                currentTenant.Id,
                from,
                until,
                cancellationToken);

            var quality = await metricsService.GetQualityMetricsAsync(
                currentTenant.Id,
                from,
                until,
                cancellationToken);

            return Results.Ok(new NotificationAnalyticsResponse(
                new NotificationAnalyticsWindow(lookbackDays, from, until),
                platform,
                interaction,
                quality));
        })
        .RequirePermission("notifications:configuration:read");
    }
}

internal sealed record NotificationAnalyticsResponse(
    NotificationAnalyticsWindow Window,
    NotificationPlatformMetrics Platform,
    NotificationInteractionMetrics Interaction,
    NotificationQualityMetrics Quality);

internal sealed record NotificationAnalyticsWindow(
    int Days,
    DateTimeOffset From,
    DateTimeOffset Until);
