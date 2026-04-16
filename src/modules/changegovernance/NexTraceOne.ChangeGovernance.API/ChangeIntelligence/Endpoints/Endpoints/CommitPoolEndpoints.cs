using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using IngestCommitFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.IngestCommit.IngestCommit;
using ListCommitsByReleaseFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListCommitsByRelease.ListCommitsByRelease;
using AddWorkItemFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.AddWorkItemToRelease.AddWorkItemToRelease;
using RemoveWorkItemFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RemoveWorkItemFromRelease.RemoveWorkItemFromRelease;
using ListWorkItemsFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListWorkItemsByRelease.ListWorkItemsByRelease;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

/// <summary>
/// Endpoints do Commit Pool e Work Item Association.
/// Expostos em:
///   POST   /api/v1/integrations/commits/ingest
///   GET    /api/v1/releases/{id}/commits
///   GET    /api/v1/releases/{id}/work-items
///   POST   /api/v1/releases/{id}/work-items
///   DELETE /api/v1/releases/{id}/work-items/{workItemAssociationId}
/// </summary>
internal static class CommitPoolEndpoints
{
    internal static void Map(
        Microsoft.AspNetCore.Routing.RouteGroupBuilder releasesGroup,
        Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        // POST /api/v1/integrations/commits/ingest
        var integrationsGroup = app.MapGroup("/api/v1/integrations/commits");

        integrationsGroup.MapPost("/ingest", async (
            IngestCommitFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(
                r => $"/api/v1/integrations/commits/{r.CommitAssociationId}",
                localizer);
        }).RequirePermission("change-intelligence:write");

        // GET /api/v1/releases/{releaseId}/commits
        releasesGroup.MapGet("/{releaseId:guid}/commits", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListCommitsByReleaseFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // GET /api/v1/releases/{releaseId}/work-items
        releasesGroup.MapGet("/{releaseId:guid}/work-items", async (
            Guid releaseId,
            bool includeRemoved,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListWorkItemsFeature.Query(releaseId, includeRemoved),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // POST /api/v1/releases/{releaseId}/work-items
        releasesGroup.MapPost("/{releaseId:guid}/work-items", async (
            Guid releaseId,
            AddWorkItemFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToCreatedResult(
                r => $"/api/v1/releases/{r.ReleaseId}/work-items/{r.WorkItemAssociationId}",
                localizer);
        }).RequirePermission("change-intelligence:write");

        // DELETE /api/v1/releases/{releaseId}/work-items/{workItemAssociationId}
        releasesGroup.MapDelete("/{releaseId:guid}/work-items/{workItemAssociationId:guid}", async (
            Guid workItemAssociationId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new RemoveWorkItemFeature.Command(workItemAssociationId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:write");
    }
}
