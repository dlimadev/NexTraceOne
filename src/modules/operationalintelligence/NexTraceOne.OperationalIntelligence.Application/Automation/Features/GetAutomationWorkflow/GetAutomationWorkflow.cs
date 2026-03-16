using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationWorkflow;

/// <summary>
/// Feature: GetAutomationWorkflow — retorna os detalhes completos de um workflow de automação,
/// incluindo pré-condições, passos de execução, validação e trilha de auditoria.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase os dados são simulados até integração completa entre módulos.
/// </summary>
public static class GetAutomationWorkflow
{
    /// <summary>Query para obter os detalhes de um workflow de automação.</summary>
    public sealed record Query(string WorkflowId) : IQuery<Response>;

    /// <summary>Valida que o identificador do workflow foi informado.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe o detalhe completo do workflow de automação.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = FindWorkflow(request.WorkflowId);
            if (response is null)
                return Task.FromResult<Result<Response>>(AutomationErrors.WorkflowNotFound(request.WorkflowId));

            return Task.FromResult(Result<Response>.Success(response));
        }

        private static Response? FindWorkflow(string workflowId)
        {
            if (workflowId.Equals("aw-0001-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    WorkflowId: Guid.Parse("b0a10001-0001-0000-0000-000000000001"),
                    ActionId: "action-restart-controlled",
                    ActionDisplayName: "Controlled Service Restart",
                    Status: AutomationWorkflowStatus.Executing,
                    RiskLevel: RiskLevel.Medium,
                    Rationale: "Payment gateway error rate exceeded 5% after last deployment — controlled restart to restore baseline.",
                    RequestedBy: "ops-engineer@nextraceone.io",
                    ApproverInfo: new ApproverInfoDto("tech-lead@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:30:00Z"), AutomationApprovalStatus.Approved),
                    Scope: "svc-payment-gateway pod group A",
                    Environment: "Production",
                    ServiceId: "svc-payment-gateway",
                    IncidentId: "a1b2c3d4-0001-0000-0000-000000000001",
                    ChangeId: "chg-deploy-2026-0042",
                    Preconditions: new[]
                    {
                        new PreconditionItem(PreconditionType.ServiceHealthCheck, "Service health check must pass before restart.", "Passed", DateTimeOffset.Parse("2024-06-15T10:25:00Z")),
                        new PreconditionItem(PreconditionType.ApprovalPresence, "Approval from Tech Lead or Architect is required.", "Passed", DateTimeOffset.Parse("2024-06-15T10:30:00Z")),
                        new PreconditionItem(PreconditionType.BlastRadiusConstraint, "Blast radius must be limited to single pod group.", "Passed", DateTimeOffset.Parse("2024-06-15T10:26:00Z")),
                    },
                    ExecutionSteps: new[]
                    {
                        new ExecutionStep(1, "Drain active connections from pod group A", "Completed", DateTimeOffset.Parse("2024-06-15T10:36:00Z"), "ops-engineer@nextraceone.io"),
                        new ExecutionStep(2, "Execute controlled restart on pod group A", "Completed", DateTimeOffset.Parse("2024-06-15T10:40:00Z"), "ops-engineer@nextraceone.io"),
                        new ExecutionStep(3, "Validate pod health after restart", "InProgress", null, null),
                        new ExecutionStep(4, "Restore traffic routing to pod group A", "Pending", null, null),
                    },
                    ValidationInfo: null,
                    AuditEntries: new[]
                    {
                        new AuditEntry(AutomationAuditAction.WorkflowCreated, "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:15:00Z"), "Workflow created for controlled restart of payment-gateway."),
                        new AuditEntry(AutomationAuditAction.PreconditionsEvaluated, "system", DateTimeOffset.Parse("2024-06-15T10:26:00Z"), "All 3 preconditions evaluated — all passed."),
                        new AuditEntry(AutomationAuditAction.ApprovalRequested, "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:27:00Z"), "Approval requested from tech-lead@nextraceone.io."),
                        new AuditEntry(AutomationAuditAction.ApprovalGranted, "tech-lead@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:30:00Z"), "Approved — low blast radius, controlled restart is safe."),
                        new AuditEntry(AutomationAuditAction.ExecutionStarted, "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:35:00Z"), "Execution started for controlled restart workflow."),
                        new AuditEntry(AutomationAuditAction.StepCompleted, "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:36:00Z"), "Step 1 completed: connections drained."),
                        new AuditEntry(AutomationAuditAction.StepCompleted, "ops-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-15T10:40:00Z"), "Step 2 completed: restart executed."),
                    },
                    CreatedAt: DateTimeOffset.Parse("2024-06-15T10:15:00Z"),
                    UpdatedAt: DateTimeOffset.Parse("2024-06-15T10:40:00Z"));
            }

            if (workflowId.Equals("aw-0002-0000-0000-000000000001", StringComparison.OrdinalIgnoreCase))
            {
                return new Response(
                    WorkflowId: Guid.Parse("b0a10002-0001-0000-0000-000000000001"),
                    ActionId: "action-observe-validate",
                    ActionDisplayName: "Observe and Validate",
                    Status: AutomationWorkflowStatus.AwaitingApproval,
                    RiskLevel: RiskLevel.Low,
                    Rationale: "Post-deployment observation requested for catalog-sync service after config update.",
                    RequestedBy: "platform-engineer@nextraceone.io",
                    ApproverInfo: null,
                    Scope: "svc-catalog-sync all instances",
                    Environment: "Production",
                    ServiceId: "svc-catalog-sync",
                    IncidentId: null,
                    ChangeId: "chg-config-2026-0100",
                    Preconditions: new[]
                    {
                        new PreconditionItem(PreconditionType.ServiceHealthCheck, "Service must be reporting healthy status.", "Pending", null),
                    },
                    ExecutionSteps: new[]
                    {
                        new ExecutionStep(1, "Collect baseline metrics snapshot", "Pending", null, null),
                        new ExecutionStep(2, "Monitor error rate for 30 minutes", "Pending", null, null),
                        new ExecutionStep(3, "Compare metrics against baseline", "Pending", null, null),
                    },
                    ValidationInfo: null,
                    AuditEntries: new[]
                    {
                        new AuditEntry(AutomationAuditAction.WorkflowCreated, "platform-engineer@nextraceone.io", DateTimeOffset.Parse("2024-06-16T08:00:00Z"), "Workflow created for post-deployment observation."),
                    },
                    CreatedAt: DateTimeOffset.Parse("2024-06-16T08:00:00Z"),
                    UpdatedAt: DateTimeOffset.Parse("2024-06-16T08:00:00Z"));
            }

            return null;
        }
    }

    /// <summary>Resposta com detalhes completos do workflow de automação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        string ActionId,
        string ActionDisplayName,
        AutomationWorkflowStatus Status,
        RiskLevel RiskLevel,
        string Rationale,
        string RequestedBy,
        ApproverInfoDto? ApproverInfo,
        string? Scope,
        string? Environment,
        string? ServiceId,
        string? IncidentId,
        string? ChangeId,
        IReadOnlyList<PreconditionItem> Preconditions,
        IReadOnlyList<ExecutionStep> ExecutionSteps,
        ValidationInfoDto? ValidationInfo,
        IReadOnlyList<AuditEntry> AuditEntries,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    /// <summary>Informação do aprovador do workflow.</summary>
    public sealed record ApproverInfoDto(
        string ApprovedBy,
        DateTimeOffset ApprovedAt,
        AutomationApprovalStatus ApprovalStatus);

    /// <summary>Item de pré-condição avaliada no workflow.</summary>
    public sealed record PreconditionItem(
        PreconditionType Type,
        string Description,
        string Status,
        DateTimeOffset? EvaluatedAt);

    /// <summary>Passo de execução do workflow de automação.</summary>
    public sealed record ExecutionStep(
        int StepOrder,
        string Title,
        string Status,
        DateTimeOffset? CompletedAt,
        string? CompletedBy);

    /// <summary>Informação de validação pós-execução.</summary>
    public sealed record ValidationInfoDto(
        ValidationStatus Status,
        string? ObservedOutcome,
        string? ValidatedBy,
        DateTimeOffset? ValidatedAt);

    /// <summary>Entrada na trilha de auditoria do workflow.</summary>
    public sealed record AuditEntry(
        AutomationAuditAction Action,
        string PerformedBy,
        DateTimeOffset PerformedAt,
        string? Details);
}
