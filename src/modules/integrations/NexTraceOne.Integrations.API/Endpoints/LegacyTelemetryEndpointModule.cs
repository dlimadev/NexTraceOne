using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using IngestBatchEventsFeature = NexTraceOne.Integrations.Application.LegacyTelemetry.Features.IngestBatchEvents.IngestBatchEvents;
using IngestMqEventsFeature = NexTraceOne.Integrations.Application.LegacyTelemetry.Features.IngestMqEvents.IngestMqEvents;
using IngestMainframeEventsFeature = NexTraceOne.Integrations.Application.LegacyTelemetry.Features.IngestMainframeEvents.IngestMainframeEvents;

namespace NexTraceOne.Integrations.API.Endpoints;

/// <summary>
/// Módulo de endpoints para ingestão de telemetria legacy mainframe.
/// Endpoints: POST /batch/events, POST /mq/events, POST /mainframe/events
///
/// Auto-descoberto pelo ApiHost via reflexão (convenção *EndpointModule + MapEndpoints).
/// </summary>
public sealed class LegacyTelemetryEndpointModule
{
    /// <summary>Registra endpoints de ingestão legacy no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var legacy = app.MapGroup("/api/v1/ingestion/legacy");

        legacy.MapPost("/batch/events", async (
            IngestBatchEventsFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:write");

        legacy.MapPost("/mq/events", async (
            IngestMqEventsFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:write");

        legacy.MapPost("/mainframe/events", async (
            IngestMainframeEventsFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:write");
    }
}
