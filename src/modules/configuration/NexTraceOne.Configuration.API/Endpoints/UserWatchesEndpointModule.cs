using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListWatches.ListWatches;
using WatchFeature = NexTraceOne.Configuration.Application.Features.WatchEntity.WatchEntity;
using UnwatchFeature = NexTraceOne.Configuration.Application.Features.UnwatchEntity.UnwatchEntity;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de watch lists — seguir entidades relevantes da plataforma.</summary>
public sealed class UserWatchesEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/watches").RequireAuthorization();

        group.MapGet("/", async (
            string? entityType,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListFeature.Query(entityType), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/", async (
            WatchFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapDelete("/{watchId:guid}", async (
            Guid watchId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new UnwatchFeature.Command(watchId), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
