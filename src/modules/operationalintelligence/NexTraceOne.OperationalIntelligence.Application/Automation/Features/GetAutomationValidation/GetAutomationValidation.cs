using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.GetAutomationValidation;

/// <summary>
/// Feature: GetAutomationValidation — retorna o estado de validação pós-execução de um workflow,
/// incluindo verificações esperadas, resultado observado e estado geral da validação.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetAutomationValidation
{
    /// <summary>Query para obter a validação de um workflow de automação.</summary>
    public sealed record Query(string WorkflowId) : IQuery<Response>;

    /// <summary>Valida que o identificador do workflow foi informado.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que compõe os dados de validação do workflow de automação.</summary>
    public sealed class Handler(
        IAutomationWorkflowRepository workflowRepository,
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

            var validationRecord = await validationRepository.GetByWorkflowIdAsync(workflowId, cancellationToken);

            if (validationRecord is null)
            {
                return Result<Response>.Success(new Response(
                    WorkflowId: workflow.Id.Value,
                    Status: ValidationStatus.Pending,
                    ObservedOutcome: null,
                    ValidatedBy: null,
                    Checks: DeriveChecksFromWorkflow(workflow, null),
                    RecordedAt: null));
            }

            var response = new Response(
                WorkflowId: workflow.Id.Value,
                Status: MapOutcomeToValidationStatus(validationRecord.Outcome),
                ObservedOutcome: validationRecord.ObservedOutcome,
                ValidatedBy: validationRecord.ValidatedBy,
                Checks: DeriveChecksFromWorkflow(workflow, validationRecord),
                RecordedAt: validationRecord.ValidatedAt);

            return Result<Response>.Success(response);
        }

        private static ValidationStatus MapOutcomeToValidationStatus(AutomationOutcome outcome) => outcome switch
        {
            AutomationOutcome.Successful => ValidationStatus.Passed,
            AutomationOutcome.Failed => ValidationStatus.Failed,
            AutomationOutcome.Cancelled => ValidationStatus.Failed,
            _ => ValidationStatus.InProgress
        };

        /// <summary>Derives validation checks from workflow and validation record state.</summary>
        private static List<ValidationCheckDto> DeriveChecksFromWorkflow(
            AutomationWorkflowRecord workflow,
            AutomationValidationRecord? validationRecord)
        {
            var checks = new List<ValidationCheckDto>();

            // Check 1: Workflow completed successfully
            var isCompleted = workflow.Status is AutomationWorkflowStatus.Completed;
            checks.Add(new ValidationCheckDto(
                CheckName: "Workflow Completion",
                IsPassed: isCompleted,
                Details: isCompleted ? "Workflow completed execution" : $"Workflow status: {workflow.Status}"));

            // Check 2: Approval was obtained (if required)
            if (workflow.ApprovalStatus != AutomationApprovalStatus.NotRequired)
            {
                var approvalPassed = workflow.ApprovalStatus == AutomationApprovalStatus.Approved;
                checks.Add(new ValidationCheckDto(
                    CheckName: "Approval Obtained",
                    IsPassed: approvalPassed,
                    Details: approvalPassed
                        ? $"Approved by {workflow.ApprovedBy} at {workflow.ApprovedAt:u}"
                        : $"Approval status: {workflow.ApprovalStatus}"));
            }

            // Check 3: Outcome validation (from validation record)
            if (validationRecord is not null)
            {
                var outcomePassed = validationRecord.Outcome == AutomationOutcome.Successful;
                checks.Add(new ValidationCheckDto(
                    CheckName: "Execution Outcome",
                    IsPassed: outcomePassed,
                    Details: validationRecord.ObservedOutcome ?? $"Outcome: {validationRecord.Outcome}"));

                // Check 4: Validator identity
                checks.Add(new ValidationCheckDto(
                    CheckName: "Validator Identity",
                    IsPassed: !string.IsNullOrEmpty(validationRecord.ValidatedBy),
                    Details: !string.IsNullOrEmpty(validationRecord.ValidatedBy)
                        ? $"Validated by {validationRecord.ValidatedBy}"
                        : "No validator recorded"));
            }

            return checks;
        }
    }

    /// <summary>Verificação individual de validação pós-execução.</summary>
    public sealed record ValidationCheckDto(
        string CheckName,
        bool IsPassed,
        string? Details);

    /// <summary>Resposta com o estado de validação pós-execução do workflow.</summary>
    public sealed record Response(
        Guid WorkflowId,
        ValidationStatus Status,
        string? ObservedOutcome,
        string? ValidatedBy,
        IReadOnlyList<ValidationCheckDto> Checks,
        DateTimeOffset? RecordedAt);
}
