using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Automation.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.UpdateAutomationWorkflowAction;

/// <summary>
/// Feature: UpdateAutomationWorkflowAction — executa uma ação sobre um workflow de automação,
/// como solicitar aprovação, aprovar, rejeitar, executar, completar passo, validar ou cancelar.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class UpdateAutomationWorkflowAction
{
    private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "request-approval", "approve", "reject", "execute", "cancel",
        "complete-step", "request-validation", "complete",
    };

    private static readonly Dictionary<string, AutomationWorkflowStatus> ActionStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["request-approval"] = AutomationWorkflowStatus.AwaitingApproval,
        ["approve"] = AutomationWorkflowStatus.Approved,
        ["reject"] = AutomationWorkflowStatus.Rejected,
        ["execute"] = AutomationWorkflowStatus.Executing,
        ["cancel"] = AutomationWorkflowStatus.Cancelled,
        ["complete-step"] = AutomationWorkflowStatus.Executing,
        ["request-validation"] = AutomationWorkflowStatus.AwaitingValidation,
        ["complete"] = AutomationWorkflowStatus.Completed,
    };

    /// <summary>Comando para executar uma ação sobre um workflow de automação.</summary>
    public sealed record Command(
        string WorkflowId,
        string Action,
        string PerformedBy,
        string? Reason,
        string? Notes) : ICommand<Response>;

    /// <summary>Valida os campos obrigatórios do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Action).NotEmpty().MaximumLength(200);
            RuleFor(x => x.PerformedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Reason).MaximumLength(1000).When(x => x.Reason is not null);
            RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes is not null);
        }
    }

    private static readonly HashSet<AutomationWorkflowStatus> TerminalStates =
    [
        AutomationWorkflowStatus.Completed,
        AutomationWorkflowStatus.Failed,
        AutomationWorkflowStatus.Cancelled,
        AutomationWorkflowStatus.Rejected
    ];

    private static readonly Dictionary<string, AutomationAuditAction> AuditActionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["request-approval"] = AutomationAuditAction.ApprovalRequested,
        ["approve"] = AutomationAuditAction.ApprovalGranted,
        ["reject"] = AutomationAuditAction.ApprovalRejected,
        ["execute"] = AutomationAuditAction.ExecutionStarted,
        ["cancel"] = AutomationAuditAction.WorkflowCancelled,
        ["complete-step"] = AutomationAuditAction.StepCompleted,
        ["request-validation"] = AutomationAuditAction.ValidationRecorded,
        ["complete"] = AutomationAuditAction.ExecutionCompleted,
    };

    /// <summary>Handler que processa a ação sobre o workflow de automação.</summary>
    public sealed class Handler(
        IAutomationWorkflowRepository workflowRepository,
        IAutomationAuditRepository auditRepository,
        IAutomationUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!AllowedActions.Contains(request.Action))
                return AutomationErrors.InvalidAction(request.Action);

            if (!Guid.TryParse(request.WorkflowId, out var parsedId))
                return AutomationErrors.WorkflowNotFound(request.WorkflowId);

            var workflowId = new AutomationWorkflowRecordId(parsedId);
            var workflow = await workflowRepository.GetByIdAsync(workflowId, cancellationToken);

            if (workflow is null)
                return AutomationErrors.WorkflowNotFound(request.WorkflowId);

            if (TerminalStates.Contains(workflow.Status))
                return AutomationErrors.WorkflowAlreadyCompleted(request.WorkflowId);

            var utcNow = clock.UtcNow;
            var newStatus = ActionStatusMap[request.Action];

            switch (request.Action.ToLowerInvariant())
            {
                case "approve":
                    workflow.Approve(request.PerformedBy, utcNow);
                    workflow.UpdateStatus(newStatus, utcNow);
                    break;
                case "reject":
                    workflow.Reject(request.PerformedBy, utcNow);
                    break;
                default:
                    workflow.UpdateStatus(newStatus, utcNow);
                    break;
            }

            await workflowRepository.UpdateAsync(workflow, cancellationToken);

            var auditAction = AuditActionMap.GetValueOrDefault(request.Action, AutomationAuditAction.StepCompleted);
            var details = $"Action '{request.Action}' performed by {request.PerformedBy}.";
            if (request.Reason is not null)
                details += $" Reason: {request.Reason}";

            var auditEntry = AutomationAuditRecord.Create(
                workflowId: workflowId,
                action: auditAction,
                actor: request.PerformedBy,
                details: details,
                utcNow: utcNow,
                serviceId: workflow.ServiceId);

            await auditRepository.AddAsync(auditEntry, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                WorkflowId: workflow.Id.Value,
                NewStatus: workflow.Status,
                ActionPerformed: request.Action,
                PerformedAt: utcNow));
        }
    }

    /// <summary>Resposta da execução de ação sobre o workflow de automação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        AutomationWorkflowStatus NewStatus,
        string ActionPerformed,
        DateTimeOffset PerformedAt);
}
