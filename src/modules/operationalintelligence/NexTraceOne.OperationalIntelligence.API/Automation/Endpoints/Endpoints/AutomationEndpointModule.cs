using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.CreateAutomationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.EvaluatePreconditions;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationAction;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationAuditTrail;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationValidation;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationWorkflow;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.ListAutomationActions;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.ListAutomationWorkflows;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.RecordAutomationValidation;
using NexTraceOne.OperationalIntelligence.Application.Automation.Features.UpdateAutomationWorkflowAction;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.API.Automation.Endpoints;

/// <summary>
/// Endpoints de Operational Automation Workflows.
/// Fornece acesso ao catálogo de ações, criação e gestão de workflows de automação,
/// aprovação, execução, pré-condições, validação pós-execução e trilha de auditoria.
/// Integra-se com Incident Correlation, Change Intelligence, Service Catalog e Source of Truth.
/// </summary>
public sealed class AutomationEndpointModule
{
    /// <summary>Mapeia os endpoints de automação operacional no pipeline HTTP.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/automation")
            .WithTags("Automation")
            .WithDescription("Operational automation workflows, action catalog, approval, execution and audit");

        // ═══════════════════════════════════════════════════════════════
        // ── Action Catalog ────────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        // ── GET /api/v1/automation/actions — Listar ações de automação disponíveis ──
        group.MapGet("/actions", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? filter,
            CancellationToken cancellationToken = default) =>
        {
            var query = new ListAutomationActions.Query(filter);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:read")
        .WithName("ListAutomationActions")
        .WithSummary("List available automation actions from the catalog");

        // ── GET /api/v1/automation/actions/{actionId} — Detalhe de uma ação ──
        group.MapGet("/actions/{actionId}", async (
            ISender sender,
            IErrorLocalizer localizer,
            string actionId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetAutomationAction.Query(actionId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:read")
        .WithName("GetAutomationAction")
        .WithSummary("Get automation action detail from the catalog");

        // ═══════════════════════════════════════════════════════════════
        // ── Workflows ─────────────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        // ── POST /api/v1/automation/workflows — Criar workflow de automação ──
        group.MapPost("/workflows", async (
            ISender sender,
            IErrorLocalizer localizer,
            CreateWorkflowRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new CreateAutomationWorkflow.Command(
                request.ActionId,
                request.ServiceId,
                request.IncidentId,
                request.ChangeId,
                request.Rationale,
                request.RequestedBy,
                request.TargetScope,
                request.TargetEnvironment);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:write")
        .WithName("CreateAutomationWorkflow")
        .WithSummary("Create a new automation workflow");

        // ── GET /api/v1/automation/workflows — Listar workflows de automação ──
        group.MapGet("/workflows", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? serviceId,
            string? status,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default) =>
        {
            var query = new ListAutomationWorkflows.Query(serviceId, status, page, pageSize);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:read")
        .WithName("ListAutomationWorkflows")
        .WithSummary("List automation workflows with optional filters");

        // ── GET /api/v1/automation/workflows/{workflowId} — Detalhe do workflow ──
        group.MapGet("/workflows/{workflowId}", async (
            ISender sender,
            IErrorLocalizer localizer,
            string workflowId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetAutomationWorkflow.Query(workflowId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:read")
        .WithName("GetAutomationWorkflow")
        .WithSummary("Get automation workflow detail");

        // ═══════════════════════════════════════════════════════════════
        // ── Workflow Actions ──────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        // ── POST /api/v1/automation/workflows/{workflowId}/request-approval — Solicitar aprovação ──
        group.MapPost("/workflows/{workflowId}/request-approval", async (
            ISender sender,
            IErrorLocalizer localizer,
            string workflowId,
            WorkflowActionRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new UpdateAutomationWorkflowAction.Command(
                workflowId, "request-approval", request.PerformedBy ?? string.Empty, request.Reason, request.Notes);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:write")
        .WithName("RequestAutomationWorkflowApproval")
        .WithSummary("Request approval for an automation workflow");

        // ── POST /api/v1/automation/workflows/{workflowId}/approve — Aprovar workflow ──
        group.MapPost("/workflows/{workflowId}/approve", async (
            ISender sender,
            IErrorLocalizer localizer,
            string workflowId,
            WorkflowActionRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new UpdateAutomationWorkflowAction.Command(
                workflowId, "approve", request.PerformedBy ?? string.Empty, request.Reason, request.Notes);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:approve")
        .WithName("ApproveAutomationWorkflow")
        .WithSummary("Approve an automation workflow");

        // ── POST /api/v1/automation/workflows/{workflowId}/reject — Rejeitar workflow ──
        group.MapPost("/workflows/{workflowId}/reject", async (
            ISender sender,
            IErrorLocalizer localizer,
            string workflowId,
            WorkflowActionRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new UpdateAutomationWorkflowAction.Command(
                workflowId, "reject", request.PerformedBy ?? string.Empty, request.Reason, request.Notes);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:approve")
        .WithName("RejectAutomationWorkflow")
        .WithSummary("Reject an automation workflow");

        // ── POST /api/v1/automation/workflows/{workflowId}/execute — Executar workflow ──
        group.MapPost("/workflows/{workflowId}/execute", async (
            ISender sender,
            IErrorLocalizer localizer,
            string workflowId,
            WorkflowActionRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new UpdateAutomationWorkflowAction.Command(
                workflowId, "execute", request.PerformedBy ?? string.Empty, request.Reason, request.Notes);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:execute")
        .WithName("ExecuteAutomationWorkflow")
        .WithSummary("Execute an automation workflow");

        // ── POST /api/v1/automation/workflows/{workflowId}/cancel — Cancelar workflow ──
        group.MapPost("/workflows/{workflowId}/cancel", async (
            ISender sender,
            IErrorLocalizer localizer,
            string workflowId,
            WorkflowActionRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new UpdateAutomationWorkflowAction.Command(
                workflowId, "cancel", request.PerformedBy ?? string.Empty, request.Reason, request.Notes);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:write")
        .WithName("CancelAutomationWorkflow")
        .WithSummary("Cancel an automation workflow");

        // ── POST /api/v1/automation/workflows/{workflowId}/complete-step — Completar passo do workflow ──
        group.MapPost("/workflows/{workflowId}/complete-step", async (
            ISender sender,
            IErrorLocalizer localizer,
            string workflowId,
            WorkflowActionRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new UpdateAutomationWorkflowAction.Command(
                workflowId, "complete-step", request.PerformedBy ?? string.Empty, request.Reason, request.Notes);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:execute")
        .WithName("CompleteAutomationWorkflowStep")
        .WithSummary("Complete a step in an automation workflow");

        // ═══════════════════════════════════════════════════════════════
        // ── Preconditions ─────────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        // ── POST /api/v1/automation/workflows/{workflowId}/evaluate-preconditions — Avaliar pré-condições ──
        group.MapPost("/workflows/{workflowId}/evaluate-preconditions", async (
            ISender sender,
            IErrorLocalizer localizer,
            string workflowId,
            EvaluatePreconditionsRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new EvaluatePreconditions.Command(workflowId, request.EvaluatedBy);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:write")
        .WithName("EvaluateAutomationPreconditions")
        .WithSummary("Evaluate preconditions for an automation workflow");

        // ═══════════════════════════════════════════════════════════════
        // ── Validation ────────────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        // ── GET /api/v1/automation/workflows/{workflowId}/validation — Obter estado de validação ──
        group.MapGet("/workflows/{workflowId}/validation", async (
            ISender sender,
            IErrorLocalizer localizer,
            string workflowId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetAutomationValidation.Query(workflowId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:read")
        .WithName("GetAutomationValidation")
        .WithSummary("Get post-execution validation status for an automation workflow");

        // ── POST /api/v1/automation/workflows/{workflowId}/validation — Registar validação pós-execução ──
        group.MapPost("/workflows/{workflowId}/validation", async (
            ISender sender,
            IErrorLocalizer localizer,
            string workflowId,
            RecordAutomationValidationRequest request,
            CancellationToken cancellationToken = default) =>
        {
            var command = new RecordAutomationValidation.Command(
                workflowId,
                request.Status,
                request.ObservedOutcome,
                request.ValidatedBy,
                request.Checks);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:write")
        .WithName("RecordAutomationValidation")
        .WithSummary("Record post-execution validation result for an automation workflow");

        // ═══════════════════════════════════════════════════════════════
        // ── Audit ─────────────────────────────────────────────────────
        // ═══════════════════════════════════════════════════════════════

        // ── GET /api/v1/automation/audit — Trilha de auditoria de automação ──
        group.MapGet("/audit", async (
            ISender sender,
            IErrorLocalizer localizer,
            string? workflowId,
            string? serviceId,
            string? teamId,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetAutomationAuditTrail.Query(workflowId, serviceId, teamId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("operations:automation:read")
        .WithName("GetAutomationAuditTrail")
        .WithSummary("Get automation audit trail with optional filters");
    }

    // ═══════════════════════════════════════════════════════════════════
    // ── Request Records (corpos HTTP) ─────────────────────────────────
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Pedido de criação de workflow de automação (corpo HTTP).</summary>
    public sealed record CreateWorkflowRequest(
        string ActionId,
        string? ServiceId,
        string? IncidentId,
        string? ChangeId,
        string Rationale,
        string RequestedBy,
        string? TargetScope,
        string? TargetEnvironment);

    /// <summary>Pedido de execução de ação sobre um workflow de automação (corpo HTTP).</summary>
    public sealed record WorkflowActionRequest(
        string? PerformedBy,
        string? Reason,
        string? Notes);

    /// <summary>Pedido de avaliação de pré-condições de um workflow (corpo HTTP).</summary>
    public sealed record EvaluatePreconditionsRequest(
        string EvaluatedBy);

    /// <summary>Pedido de registo de validação pós-execução de automação (corpo HTTP).</summary>
    public sealed record RecordAutomationValidationRequest(
        ValidationStatus Status,
        string? ObservedOutcome,
        string? ValidatedBy,
        IReadOnlyList<RecordAutomationValidation.ValidationCheckInput>? Checks);
}
