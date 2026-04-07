using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using GetPrefsFeature = NexTraceOne.Configuration.Application.Features.GetUserPreferences.GetUserPreferences;
using SetPrefFeature = NexTraceOne.Configuration.Application.Features.SetUserPreference.SetUserPreference;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>
/// Endpoints de preferências do utilizador — personalização de sidebar, home, widgets.
/// Operam no scope User do sistema de configuração, respeitando limites da plataforma.
/// </summary>
public sealed class UserPreferencesEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/user-preferences").RequireAuthorization();

        // GET /api/v1/user-preferences?prefix=platform.
        group.MapGet("/", async (
            string? prefix,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetPrefsFeature.Query(prefix), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        // PUT /api/v1/user-preferences
        group.MapPut("/", async (
            SetPrefFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
