using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using RequestApprovalFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RequestExternalApproval.RequestExternalApproval;
using RespondToApprovalFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RespondToApprovalRequest.RespondToApprovalRequest;
using ListApprovalsFeature = NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListApprovalRequests.ListApprovalRequests;

namespace NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints.Endpoints;

/// <summary>
/// Endpoints do External Approval Gateway.
/// Expostos em:
///   POST /api/v1/releases/{id}/approval-requests
///   GET  /api/v1/releases/{id}/approval-requests
///   POST /api/v1/releases/{id}/approvals/{token}/respond   (callback inbound, público)
/// </summary>
internal static class ApprovalGatewayEndpoints
{
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        // POST /api/v1/releases/{releaseId}/approval-requests — cria pedido de aprovação
        group.MapPost("/{releaseId:guid}/approval-requests", async (
            Guid releaseId,
            RequestApprovalFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ReleaseId = releaseId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToCreatedResult(
                r => $"/api/v1/releases/{r.ReleaseId}/approval-requests/{r.ApprovalRequestId}",
                localizer);
        }).RequirePermission("change-intelligence:write");

        // GET /api/v1/releases/{releaseId}/approval-requests — lista pedidos de aprovação
        group.MapGet("/{releaseId:guid}/approval-requests", async (
            Guid releaseId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListApprovalsFeature.Query(releaseId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("change-intelligence:read");

        // POST /api/v1/releases/{releaseId}/approvals/{callbackToken}/respond
        // Endpoint público (autenticado pelo callback token, não por user auth)
        // Usado por sistemas externos para responder ao pedido de aprovação outbound
        group.MapPost("/{releaseId:guid}/approvals/{callbackToken}/respond", async (
            Guid releaseId,
            string callbackToken,
            RespondToApprovalFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { CallbackToken = callbackToken };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).AllowAnonymous(); // autenticado pelo callback token — não requer user session
    }
}
