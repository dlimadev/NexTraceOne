using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListSavedPrompts.ListSavedPrompts;
using SaveFeature = NexTraceOne.Configuration.Application.Features.SavePrompt.SavePrompt;
using ShareFeature = NexTraceOne.Configuration.Application.Features.ShareSavedPrompt.ShareSavedPrompt;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteSavedPrompt.DeleteSavedPrompt;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de prompts de IA guardados pelo utilizador.</summary>
public sealed class SavedPromptsEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/saved-prompts").RequireAuthorization();

        group.MapGet("/", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/", async (
            SaveFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPatch("/{promptId:guid}/share", async (
            Guid promptId,
            ShareRequest body,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ShareFeature.Command(promptId, body.IsShared), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapDelete("/{promptId:guid}", async (
            Guid promptId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteFeature.Command(promptId), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }

    private sealed record ShareRequest(bool IsShared);
}
