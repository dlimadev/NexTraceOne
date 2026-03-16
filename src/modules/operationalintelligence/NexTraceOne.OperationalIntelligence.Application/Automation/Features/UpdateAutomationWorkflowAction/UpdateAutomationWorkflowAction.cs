using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Automation.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Automation.Features.UpdateAutomationWorkflowAction;

/// <summary>
/// Feature: UpdateAutomationWorkflowAction — executa uma ação sobre um workflow de automação,
/// como solicitar aprovação, aprovar, rejeitar, executar, completar passo, validar ou cancelar.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
///
/// Nota: nesta fase os dados são simulados até integração completa entre módulos.
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

    /// <summary>Handler que processa a ação sobre o workflow de automação.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!AllowedActions.Contains(request.Action))
                return Task.FromResult<Result<Response>>(AutomationErrors.InvalidAction(request.Action));

            var newStatus = ActionStatusMap[request.Action];

            var response = new Response(
                WorkflowId: Guid.TryParse(request.WorkflowId, out var wfId) ? wfId : Guid.NewGuid(),
                NewStatus: newStatus,
                ActionPerformed: request.Action,
                PerformedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta da execução de ação sobre o workflow de automação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        AutomationWorkflowStatus NewStatus,
        string ActionPerformed,
        DateTimeOffset PerformedAt);
}
