using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using InitiateWorkflowFeature = NexTraceOne.Workflow.Application.Features.InitiateWorkflow.InitiateWorkflow;
using ApproveStageFeature = NexTraceOne.Workflow.Application.Features.ApproveStage.ApproveStage;
using RejectWorkflowFeature = NexTraceOne.Workflow.Application.Features.RejectWorkflow.RejectWorkflow;
using RequestChangesFeature = NexTraceOne.Workflow.Application.Features.RequestChanges.RequestChanges;
using AddObservationFeature = NexTraceOne.Workflow.Application.Features.AddObservation.AddObservation;

namespace NexTraceOne.Workflow.API.Endpoints;

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
            return result.ToCreatedResult("/api/v1/workflow/{0}", localizer);
        });

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
        });

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
        });

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
        });

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
        });
    }
}
