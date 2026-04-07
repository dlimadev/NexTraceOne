using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListScheduledReports.ListScheduledReports;
using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateScheduledReport.CreateScheduledReport;
using ToggleFeature = NexTraceOne.Configuration.Application.Features.ToggleScheduledReport.ToggleScheduledReport;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteScheduledReport.DeleteScheduledReport;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de relatórios programados por utilizador.</summary>
public sealed class ScheduledReportsEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/scheduled-reports").RequireAuthorization();

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

        group.MapPatch("/{id:guid}/toggle", async (
            Guid id,
            ToggleFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var cmd = command with { ReportId = id };
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
