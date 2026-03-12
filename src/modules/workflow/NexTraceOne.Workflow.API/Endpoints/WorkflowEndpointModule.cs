using MediatR;
using Microsoft.AspNetCore.Builder;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using CreateWorkflowTemplateFeature = NexTraceOne.Workflow.Application.Features.CreateWorkflowTemplate.CreateWorkflowTemplate;
using InitiateWorkflowFeature = NexTraceOne.Workflow.Application.Features.InitiateWorkflow.InitiateWorkflow;
using ApproveStageFeature = NexTraceOne.Workflow.Application.Features.ApproveStage.ApproveStage;
using RejectWorkflowFeature = NexTraceOne.Workflow.Application.Features.RejectWorkflow.RejectWorkflow;
using RequestChangesFeature = NexTraceOne.Workflow.Application.Features.RequestChanges.RequestChanges;
using AddObservationFeature = NexTraceOne.Workflow.Application.Features.AddObservation.AddObservation;
using GetWorkflowStatusFeature = NexTraceOne.Workflow.Application.Features.GetWorkflowStatus.GetWorkflowStatus;
using ListPendingApprovalsFeature = NexTraceOne.Workflow.Application.Features.ListPendingApprovals.ListPendingApprovals;
using GenerateEvidencePackFeature = NexTraceOne.Workflow.Application.Features.GenerateEvidencePack.GenerateEvidencePack;
using GetEvidencePackFeature = NexTraceOne.Workflow.Application.Features.GetEvidencePack.GetEvidencePack;
using ExportEvidencePackPdfFeature = NexTraceOne.Workflow.Application.Features.ExportEvidencePackPdf.ExportEvidencePackPdf;
using EscalateSlaViolationFeature = NexTraceOne.Workflow.Application.Features.EscalateSlaViolation.EscalateSlaViolation;

namespace NexTraceOne.Workflow.API.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Workflow.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// </summary>
public sealed class WorkflowEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/workflow");

        group.MapPost("/templates", async (
            CreateWorkflowTemplateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/workflow/templates/{0}", localizer);
        });

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

        group.MapGet("/{instanceId:guid}/status", async (
            Guid instanceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetWorkflowStatusFeature.Query(instanceId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/pending-approvals", async (
            string approverUserId,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListPendingApprovalsFeature.Query(approverUserId, page, pageSize), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/{instanceId:guid}/evidence-pack", async (
            Guid instanceId,
            GenerateEvidencePackFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { WorkflowInstanceId = instanceId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToCreatedResult("/api/v1/workflow/{0}/evidence-pack", localizer);
        });

        group.MapGet("/{instanceId:guid}/evidence-pack", async (
            Guid instanceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetEvidencePackFeature.Query(instanceId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/{instanceId:guid}/evidence-pack/export", async (
            Guid instanceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ExportEvidencePackPdfFeature.Query(instanceId), cancellationToken);
            return result.ToHttpResult(localizer);
        });

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
        });
    }
}
