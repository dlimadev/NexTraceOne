using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationWorkflow;

/// <summary>
/// Feature: GetAutomationWorkflow — retorna os detalhes completos de um workflow de automação,
/// incluindo pré-condições, passos de execução, validação e trilha de auditoria.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
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
    public sealed class Handler(
        IAutomationWorkflowRepository workflowRepository,
        IAutomationAuditRepository auditRepository,
        IAutomationValidationRepository validationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.WorkflowId, out var parsedId))
                return AutomationErrors.WorkflowNotFound(request.WorkflowId);

            var workflowId = new AutomationWorkflowRecordId(parsedId);
            var workflow = await workflowRepository.GetByIdAsync(workflowId, cancellationToken);

            if (workflow is null)
                return AutomationErrors.WorkflowNotFound(request.WorkflowId);

            var auditRecords = await auditRepository.GetByWorkflowIdAsync(workflowId, cancellationToken);
            var validationRecord = await validationRepository.GetByWorkflowIdAsync(workflowId, cancellationToken);

            var actionDisplayName = AutomationActionCatalog.GetAll()
                .FirstOrDefault(a => a.ActionId.Equals(workflow.ActionId, StringComparison.OrdinalIgnoreCase))
                ?.DisplayName ?? workflow.ActionId;

            ApproverInfoDto? approverInfo = workflow.ApprovedBy is not null && workflow.ApprovedAt.HasValue
                ? new ApproverInfoDto(workflow.ApprovedBy, workflow.ApprovedAt.Value, workflow.ApprovalStatus)
                : null;

            ValidationInfoDto? validationInfo = validationRecord is not null
                ? new ValidationInfoDto(
                    MapOutcomeToValidationStatus(validationRecord.Outcome),
                    validationRecord.ObservedOutcome,
                    validationRecord.ValidatedBy,
                    validationRecord.ValidatedAt)
                : null;

            var auditEntries = auditRecords
                .Select(a => new AuditEntry(a.Action, a.Actor, a.OccurredAt, a.Details))
                .ToList();

            var response = new Response(
                WorkflowId: workflow.Id.Value,
                ActionId: workflow.ActionId,
                ActionDisplayName: actionDisplayName,
                Status: workflow.Status,
                RiskLevel: workflow.RiskLevel,
                Rationale: workflow.Rationale,
                RequestedBy: workflow.RequestedBy,
                ApproverInfo: approverInfo,
                Scope: workflow.TargetScope,
                Environment: workflow.TargetEnvironment,
                ServiceId: workflow.ServiceId,
                IncidentId: workflow.IncidentId,
                ChangeId: workflow.ChangeId,
                Preconditions: DerivePreconditions(workflow),
                ExecutionSteps: DeriveExecutionSteps(workflow, validationRecord),
                ValidationInfo: validationInfo,
                AuditEntries: auditEntries,
                CreatedAt: workflow.CreatedAt,
                UpdatedAt: workflow.UpdatedAt);

            return Result<Response>.Success(response);
        }

        private static ValidationStatus MapOutcomeToValidationStatus(AutomationOutcome outcome) => outcome switch
        {
            AutomationOutcome.Successful => ValidationStatus.Passed,
            AutomationOutcome.Failed => ValidationStatus.Failed,
            AutomationOutcome.Cancelled => ValidationStatus.Failed,
            _ => ValidationStatus.InProgress
        };

        /// <summary>Derives preconditions from the current workflow state.</summary>
        private static List<PreconditionItem> DerivePreconditions(AutomationWorkflowRecord workflow)
        {
            var preconditions = new List<PreconditionItem>();

            // Risk assessment / blast radius precondition
            var riskStatus = workflow.RiskLevel is RiskLevel.Critical or RiskLevel.High
                ? "RequiresReview"
                : "Passed";
            preconditions.Add(new PreconditionItem(
                Type: PreconditionType.BlastRadiusConstraint,
                Description: $"Risk level assessed as {workflow.RiskLevel}",
                Status: riskStatus,
                EvaluatedAt: workflow.CreatedAt));

            // Approval precondition (for non-low risk)
            if (workflow.RiskLevel != RiskLevel.Low)
            {
                var approvalStatus = workflow.ApprovalStatus switch
                {
                    AutomationApprovalStatus.Approved => "Passed",
                    AutomationApprovalStatus.Rejected => "Failed",
                    AutomationApprovalStatus.NotRequired => "Passed",
                    _ => "Pending"
                };
                preconditions.Add(new PreconditionItem(
                    Type: PreconditionType.ApprovalPresence,
                    Description: "Workflow requires approval before execution",
                    Status: approvalStatus,
                    EvaluatedAt: workflow.ApprovedAt));
            }

            // Service health precondition
            if (!string.IsNullOrEmpty(workflow.ServiceId))
            {
                preconditions.Add(new PreconditionItem(
                    Type: PreconditionType.ServiceHealthCheck,
                    Description: $"Target service {workflow.ServiceId} must be operational",
                    Status: "Passed",
                    EvaluatedAt: workflow.CreatedAt));
            }

            // Environment restriction precondition
            if (!string.IsNullOrEmpty(workflow.TargetEnvironment))
            {
                preconditions.Add(new PreconditionItem(
                    Type: PreconditionType.EnvironmentRestriction,
                    Description: $"Target environment {workflow.TargetEnvironment} must be accessible",
                    Status: "Passed",
                    EvaluatedAt: workflow.CreatedAt));
            }

            return preconditions;
        }

        /// <summary>Derives execution steps from the workflow lifecycle state.</summary>
        private static List<ExecutionStep> DeriveExecutionSteps(
            AutomationWorkflowRecord workflow,
            AutomationValidationRecord? validationRecord)
        {
            var steps = new List<ExecutionStep>();
            var isCompleted = workflow.Status is AutomationWorkflowStatus.Completed;
            var isFailed = workflow.Status is AutomationWorkflowStatus.Failed;
            var isExecuting = workflow.Status is AutomationWorkflowStatus.Executing;

            // Step 1: Request & Rationale
            steps.Add(new ExecutionStep(
                StepOrder: 1,
                Title: "Request Submitted",
                Status: "Completed",
                CompletedAt: workflow.CreatedAt,
                CompletedBy: workflow.RequestedBy));

            // Step 2: Approval (if applicable)
            if (workflow.ApprovalStatus != AutomationApprovalStatus.NotRequired)
            {
                var approvalCompleted = workflow.ApprovedAt.HasValue;
                steps.Add(new ExecutionStep(
                    StepOrder: 2,
                    Title: "Approval",
                    Status: approvalCompleted
                        ? workflow.ApprovalStatus == AutomationApprovalStatus.Rejected ? "Failed" : "Completed"
                        : "Pending",
                    CompletedAt: workflow.ApprovedAt,
                    CompletedBy: workflow.ApprovedBy));
            }

            // Step 3: Execution
            var executionStatus = isCompleted || isFailed ? (isFailed ? "Failed" : "Completed")
                : isExecuting ? "InProgress" : "Pending";
            steps.Add(new ExecutionStep(
                StepOrder: steps.Count + 1,
                Title: "Action Execution",
                Status: executionStatus,
                CompletedAt: isCompleted || isFailed ? workflow.UpdatedAt : null,
                CompletedBy: null));

            // Step 4: Validation
            if (validationRecord is not null)
            {
                steps.Add(new ExecutionStep(
                    StepOrder: steps.Count + 1,
                    Title: "Post-Execution Validation",
                    Status: validationRecord.Outcome == AutomationOutcome.Successful ? "Completed" : "Failed",
                    CompletedAt: validationRecord.ValidatedAt,
                    CompletedBy: validationRecord.ValidatedBy));
            }
            else if (isCompleted || isFailed)
            {
                steps.Add(new ExecutionStep(
                    StepOrder: steps.Count + 1,
                    Title: "Post-Execution Validation",
                    Status: "Pending",
                    CompletedAt: null,
                    CompletedBy: null));
            }

            return steps;
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
