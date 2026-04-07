using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListWebhookTemplates.ListWebhookTemplates;
using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateWebhookTemplate.CreateWebhookTemplate;
using ToggleFeature = NexTraceOne.Configuration.Application.Features.ToggleWebhookTemplate.ToggleWebhookTemplate;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteWebhookTemplate.DeleteWebhookTemplate;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de templates de webhook personalizados do tenant.</summary>
public sealed class WebhookTemplatesEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/webhook-templates").RequireAuthorization();

        group.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/", async (
            CreateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPatch("/{templateId:guid}/toggle", async (
            Guid templateId,
            ToggleRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ToggleFeature.Command(templateId, body.Enabled), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapDelete("/{templateId:guid}", async (
            Guid templateId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteFeature.Command(templateId), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }

    private sealed record ToggleRequest(bool Enabled);
}
