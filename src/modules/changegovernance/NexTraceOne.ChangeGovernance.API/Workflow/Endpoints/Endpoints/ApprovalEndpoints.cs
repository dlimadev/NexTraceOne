using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using InitiateWorkflowFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.InitiateWorkflow.InitiateWorkflow;
using ApproveStageFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.ApproveStage.ApproveStage;
using RejectWorkflowFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.RejectWorkflow.RejectWorkflow;
using RequestChangesFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.RequestChanges.RequestChanges;
using AddObservationFeature = NexTraceOne.ChangeGovernance.Application.Workflow.Features.AddObservation.AddObservation;

namespace NexTraceOne.ChangeGovernance.API.Workflow.Endpoints.Endpoints;

/// <summary>
/// Endpoints de aprovação e decisão no ciclo de vida do workflow.
/// Inclui a criação de instâncias de workflow, aprovação de stages,
/// rejeição, solicitação de alterações e adição de observações.
///
/// Cada ação delega para o handler MediatR correspondente, mantendo os
/// endpoints finos e sem regra de negócio (padrão REPR).
/// </summary>
internal static class ApprovalEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de aprovação e decisão diretamente no grupo raiz do workflow.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            InitiateWorkflowFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/v1/workflow/{r.WorkflowInstanceId}", localizer);
        }).RequirePermission("workflow:instances:write");

        group.MapPost("/stages/{stageId:guid}/approve", async (
            Guid stageId,
            ApproveStageFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { StageId = stageId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("workflow:instances:write");

        group.MapPost("/{instanceId:guid}/reject", async (
            Guid instanceId,
            RejectWorkflowFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { InstanceId = instanceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("workflow:instances:write");

        group.MapPost("/{instanceId:guid}/stages/{stageId:guid}/request-changes", async (
            Guid instanceId,
            Guid stageId,
            RequestChangesFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { InstanceId = instanceId, StageId = stageId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("workflow:instances:write");

        group.MapPost("/{instanceId:guid}/stages/{stageId:guid}/observation", async (
            Guid instanceId,
            Guid stageId,
            AddObservationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { InstanceId = instanceId, StageId = stageId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("workflow:instances:write");
    }
}
