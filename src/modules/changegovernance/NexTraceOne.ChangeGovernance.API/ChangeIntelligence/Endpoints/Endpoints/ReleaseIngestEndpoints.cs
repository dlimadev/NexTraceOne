using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using IngestExternalReleaseFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IngestExternalRelease.IngestExternalRelease;
using GetImpactReportFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.GetReleaseImpactReport.GetReleaseImpactReport;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

/// <summary>
/// Endpoints de ingestão externa de releases e relatório de impacto.
/// Expostos em:
///   POST /api/v1/releases/ingest             — ingestão de release de sistema externo
///   GET  /api/v1/releases/{id}/impact-report — relatório de impacto calculado
/// </summary>
internal static class ReleaseIngestEndpoints
{
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        // POST /api/v1/releases/ingest
        // Recebe release de sistemas externos (AzureDevOps, Jira, Jenkins, GitLab)
        group.MapPost("/ingest", async (
            IngestExternalReleaseFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                r => $"/api/v1/releases/{r.ReleaseId}",
                localizer);
        }).RequirePermission("change-intelligence:write");

        // GET /api/v1/releases/{releaseId}/impact-report
        // Calcula e devolve o relatório de impacto de uma release
        group.MapGet("/{releaseId:guid}/impact-report", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetImpactReportFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");
    }
}
