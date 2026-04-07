using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.Configuration.Domain.Enums;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListBookmarks.ListBookmarks;
using AddFeature = NexTraceOne.Configuration.Application.Features.AddBookmark.AddBookmark;
using RemoveFeature = NexTraceOne.Configuration.Application.Features.RemoveBookmark.RemoveBookmark;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de favoritos de entidades da plataforma por utilizador.</summary>
public sealed class UserBookmarksEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/bookmarks").RequireAuthorization();

        group.MapGet("/", async (
            BookmarkEntityType? entityType,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListFeature.Query(entityType), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/", async (
            AddFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RemoveFeature.Command(id), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
