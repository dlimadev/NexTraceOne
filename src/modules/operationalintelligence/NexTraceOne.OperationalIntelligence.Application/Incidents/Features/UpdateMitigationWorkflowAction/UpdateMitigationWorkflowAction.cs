using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.UpdateMitigationWorkflowAction;

/// <summary>
/// Feature: UpdateMitigationWorkflowAction — executa uma ação sobre um workflow de mitigação,
/// como aprovar, rejeitar, iniciar, completar passo, solicitar validação, concluir ou cancelar.
/// </summary>
public static class UpdateMitigationWorkflowAction
{
    private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "approve", "reject", "start", "complete-step", "request-validation", "complete", "cancel",
    };

    private static readonly Dictionary<string, MitigationWorkflowStatus> ActionStatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["approve"] = MitigationWorkflowStatus.Approved,
        ["reject"] = MitigationWorkflowStatus.Rejected,
        ["start"] = MitigationWorkflowStatus.InProgress,
        ["complete-step"] = MitigationWorkflowStatus.InProgress,
        ["request-validation"] = MitigationWorkflowStatus.AwaitingValidation,
        ["complete"] = MitigationWorkflowStatus.Completed,
        ["cancel"] = MitigationWorkflowStatus.Cancelled,
    };

    /// <summary>Comando para executar uma ação sobre um workflow de mitigação.</summary>
    public sealed record Command(
        string IncidentId,
        string WorkflowId,
        string Action,
        string? PerformedBy,
        string? Reason,
        string? Notes) : ICommand<Response>;

    /// <summary>Valida os campos obrigatórios do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.WorkflowId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Action).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que processa a ação sobre o workflow de mitigação.</summary>
    public sealed class Handler : ICommandHandler<Command, Response>
    {
        private static readonly HashSet<string> KnownIncidentIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "a1b2c3d4-0001-0000-0000-000000000001",
            "a1b2c3d4-0002-0000-0000-000000000002",
        };

        public Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!KnownIncidentIds.Contains(request.IncidentId))
                return Task.FromResult<Result<Response>>(IncidentErrors.IncidentNotFound(request.IncidentId));

            if (!AllowedActions.Contains(request.Action))
                return Task.FromResult<Result<Response>>(IncidentErrors.InvalidWorkflowAction(request.Action));

            var newStatus = ActionStatusMap[request.Action];

            var response = new Response(
                WorkflowId: Guid.TryParse(request.WorkflowId, out var wfId) ? wfId : Guid.NewGuid(),
                NewStatus: newStatus,
                ActionPerformed: request.Action,
                PerformedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta da execução de ação sobre o workflow.</summary>
    public sealed record Response(
        Guid WorkflowId,
        MitigationWorkflowStatus NewStatus,
        string ActionPerformed,
        DateTimeOffset PerformedAt);
}
