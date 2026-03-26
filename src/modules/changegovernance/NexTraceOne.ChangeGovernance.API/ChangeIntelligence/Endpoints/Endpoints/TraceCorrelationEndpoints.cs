using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using RecordTraceCorrelationFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordTraceCorrelation.RecordTraceCorrelation;
using GetTraceCorrelationsFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetTraceCorrelations.GetTraceCorrelations;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

/// <summary>
/// Endpoints de correlação trace → release.
/// Permitem associar traces OTel a releases e consultar quais traces estão
/// correlacionados a uma release específica.
///
/// POST /api/v1/releases/{releaseId}/traces — regista correlação automática
/// GET  /api/v1/releases/{releaseId}/traces — lista traces correlacionados
/// </summary>
internal static class TraceCorrelationEndpoints
{
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapPost("/{releaseId:guid}/traces", async (
            Guid releaseId,
            RecordTraceCorrelationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");

        group.MapGet("/{releaseId:guid}/traces", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetTraceCorrelationsFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");
    }
}
