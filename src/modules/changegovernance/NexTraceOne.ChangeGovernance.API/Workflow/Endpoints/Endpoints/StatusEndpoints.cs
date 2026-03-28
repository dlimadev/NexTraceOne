using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GetWorkflowStatusFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.GetWorkflowStatus.GetWorkflowStatus;
using ListPendingApprovalsFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.ListPendingApprovals.ListPendingApprovals;
using EscalateSlaViolationFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.EscalateSlaViolation.EscalateSlaViolation;

namespace NexTraceOne.ChangeGovernance.API.Workflow.Endpoints.Endpoints;

/// <summary>
/// Endpoints de consulta de status e monitoramento de workflows.
/// Inclui consulta de status individual, listagem de aprovações pendentes
/// e escalação de violações de SLA.
///
/// Estes endpoints suportam tanto o dashboard do Tech Lead quanto a
/// visão operacional de plataforma, alimentando o painel de pendências.
/// </summary>
internal static class StatusEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de status e monitoramento no grupo raiz do workflow.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapGet("/{instanceId:guid}/status", async (
            Guid instanceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetWorkflowStatusFeature.Query(instanceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("workflow:instances:read");

        group.MapGet("/pending-approvals", async (
            string approverUserId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(new ListPendingApprovalsFeature.Query(approverUserId, page, pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("workflow:instances:read");

        group.MapPost("/{instanceId:guid}/escalate-sla", async (
            Guid instanceId,
            EscalateSlaViolationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { WorkflowInstanceId = instanceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("workflow:instances:write");
    }
}
