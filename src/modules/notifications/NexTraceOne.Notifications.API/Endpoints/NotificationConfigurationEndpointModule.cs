using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

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
            ISender sender,
            IErrorLocalizer localizer,
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
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:configuration:write");

        // ── Channels ──────────────────────────────────────────────────────────

        group.MapGet("/channels", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListChannelsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:configuration:read");

        group.MapPut("/channels", async (
            UpsertChannelFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:configuration:write");

        // ── SMTP ──────────────────────────────────────────────────────────────

        group.MapGet("/smtp", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetSmtpFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:configuration:read");

        group.MapPut("/smtp", async (
            UpsertSmtpFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("notifications:configuration:write");
    }
}
