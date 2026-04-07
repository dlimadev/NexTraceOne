using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListUserSavedViews.ListUserSavedViews;
using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateUserSavedView.CreateUserSavedView;
using UpdateFeature = NexTraceOne.Configuration.Application.Features.UpdateUserSavedView.UpdateUserSavedView;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteUserSavedView.DeleteUserSavedView;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de vistas guardadas por utilizador — personalização de filtros e colunas por contexto.</summary>
public sealed class UserSavedViewsEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/user-saved-views").RequireAuthorization();

        group.MapGet("/", async (
            string? context,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListFeature.Query(context), cancellationToken);
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

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { Id = id };
            var result = await sender.Send(cmd, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteFeature.Command(id), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
