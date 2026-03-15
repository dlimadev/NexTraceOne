using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationHistory;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationRecommendations;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetMitigationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.RecordMitigationValidation;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Features.UpdateMitigationWorkflowAction;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.API.Incidents.Endpoints;

/// <summary>
/// Endpoints de Operational Mitigation Workflows.
/// Fornece acesso a recomendações de mitigação, workflows, histórico e validação pós-mitigação.
/// Integra-se com Incident Correlation, Runbooks, Change Intelligence e Source of Truth.
/// </summary>
public sealed class MitigationEndpointModule
{
    /// <summary>Mapeia os endpoints de mitigação no pipeline HTTP.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/incidents/{incidentId}/mitigation")
            .WithTags("Mitigation")
            .WithDescription("Operational mitigation workflows, recommendations and validation");

        // ── GET /api/v1/incidents/{incidentId}/mitigation/recommendations — Recomendações de mitigação ──
        group.MapGet("/recommendations", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetMitigationRecommendations.Query(incidentId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:mitigation:read")
        .WithName("GetMitigationRecommendations")
        .WithSummary("Get mitigation recommendations for an incident");

        // ── GET /api/v1/incidents/{incidentId}/mitigation/workflows/{workflowId} — Detalhe do workflow ──
        group.MapGet("/workflows/{workflowId}", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            string workflowId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetMitigationWorkflow.Query(incidentId, workflowId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:mitigation:read")
        .WithName("GetMitigationWorkflow")
        .WithSummary("Get mitigation workflow detail");

        // ── POST /api/v1/incidents/{incidentId}/mitigation/workflows — Criar workflow de mitigação ──
        group.MapPost("/workflows", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            CreateMitigationWorkflowRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new CreateMitigationWorkflow.Command(
                incidentId,
                request.Title,
                request.ActionType,
                request.RiskLevel,
                request.RequiresApproval,
                request.LinkedRunbookId,
                request.Steps);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:mitigation:write")
        .WithName("CreateMitigationWorkflow")
        .WithSummary("Create a new mitigation workflow for an incident");

        // ── PATCH /api/v1/incidents/{incidentId}/mitigation/workflows/{workflowId}/actions — Executar ação no workflow ──
        group.MapPatch("/workflows/{workflowId}/actions", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            string workflowId,
            UpdateMitigationWorkflowActionRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new UpdateMitigationWorkflowAction.Command(
                incidentId,
                workflowId,
                request.Action,
                request.PerformedBy,
                request.Reason,
                request.Notes);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:mitigation:write")
        .WithName("UpdateMitigationWorkflowAction")
        .WithSummary("Execute an action on a mitigation workflow");

        // ── GET /api/v1/incidents/{incidentId}/mitigation/history — Histórico de mitigação ──
        group.MapGet("/history", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetMitigationHistory.Query(incidentId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:mitigation:read")
        .WithName("GetMitigationHistory")
        .WithSummary("Get mitigation history for an incident");

        // ── GET /api/v1/incidents/{incidentId}/mitigation/workflows/{workflowId}/validation — Validação do workflow ──
        group.MapGet("/workflows/{workflowId}/validation", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            string workflowId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetMitigationValidation.Query(incidentId, workflowId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:mitigation:read")
        .WithName("GetMitigationValidation")
        .WithSummary("Get post-mitigation validation status");

        // ── POST /api/v1/incidents/{incidentId}/mitigation/workflows/{workflowId}/validation — Registar validação ──
        group.MapPost("/workflows/{workflowId}/validation", async (
            ISender sender,
            IErrorLocalizer localizer,
            string incidentId,
            string workflowId,
            RecordMitigationValidationRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new RecordMitigationValidation.Command(
                incidentId,
                workflowId,
                request.Status,
                request.ObservedOutcome,
                request.ValidatedBy,
                request.Checks);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:mitigation:write")
        .WithName("RecordMitigationValidation")
        .WithSummary("Record post-mitigation validation result");
    }

    /// <summary>Pedido de criação de workflow de mitigação (corpo HTTP).</summary>
    public sealed record CreateMitigationWorkflowRequest(
        string Title,
        MitigationActionType ActionType,
        RiskLevel RiskLevel,
        bool RequiresApproval,
        Guid? LinkedRunbookId,
        IReadOnlyList<CreateMitigationWorkflow.CreateStepDto>? Steps);

    /// <summary>Pedido de execução de ação sobre um workflow de mitigação (corpo HTTP).</summary>
    public sealed record UpdateMitigationWorkflowActionRequest(
        string Action,
        string? PerformedBy,
        string? Reason,
        string? Notes);

    /// <summary>Pedido de registo de validação pós-mitigação (corpo HTTP).</summary>
    public sealed record RecordMitigationValidationRequest(
        ValidationStatus Status,
        string? ObservedOutcome,
        string? ValidatedBy,
        IReadOnlyList<RecordMitigationValidation.ValidationCheckInput>? Checks);
}
