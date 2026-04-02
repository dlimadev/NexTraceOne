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
                Preconditions: Array.Empty<PreconditionItem>(),
                ExecutionSteps: Array.Empty<ExecutionStep>(),
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
