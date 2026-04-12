using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListEntityTags.ListEntityTags;
using AddFeature = NexTraceOne.Configuration.Application.Features.AddEntityTag.AddEntityTag;
using RemoveFeature = NexTraceOne.Configuration.Application.Features.RemoveEntityTag.RemoveEntityTag;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de tags transversais de entidades da plataforma.</summary>
public sealed class EntityTagsEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tags").RequireAuthorization();

        group.MapGet("/", async (
            string tenantId,
            string entityType,
            string entityId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListFeature.Query(tenantId, entityType, entityId), cancellationToken);
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

        group.MapDelete("/{tagId:guid}", async (
            Guid tagId,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new RemoveFeature.Command(tagId, tenantId), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
