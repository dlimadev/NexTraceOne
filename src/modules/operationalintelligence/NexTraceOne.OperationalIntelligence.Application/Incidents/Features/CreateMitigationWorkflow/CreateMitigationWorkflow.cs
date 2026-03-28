using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Enums;
using NexTraceOne.OperationalIntelligence.Domain.Incidents.Errors;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.CreateMitigationWorkflow;

/// <summary>
/// Feature: CreateMitigationWorkflow — cria um novo workflow de mitigação para um incidente,
/// definindo tipo de ação, nível de risco, passos e associação a runbooks.
/// </summary>
public static class CreateMitigationWorkflow
{
    /// <summary>Comando para criar um workflow de mitigação.</summary>
    public sealed record Command(
        string IncidentId,
        string Title,
        MitigationActionType ActionType,
        RiskLevel RiskLevel,
        bool RequiresApproval,
        Guid? LinkedRunbookId,
        IReadOnlyList<CreateStepDto>? Steps) : ICommand<Response>;

    /// <summary>Passo a incluir na criação do workflow.</summary>
    public sealed record CreateStepDto(int StepOrder, string Title, string? Description);

    /// <summary>Valida os campos obrigatórios do comando de criação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IncidentId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ActionType).IsInEnum();
            RuleFor(x => x.RiskLevel).IsInEnum();
        }
    }

    /// <summary>Handler que cria o workflow de mitigação via store.</summary>
    public sealed class Handler(IIncidentStore store) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!store.IncidentExists(request.IncidentId))
                return IncidentErrors.IncidentNotFound(request.IncidentId);

            var response = await store.CreateMitigationWorkflowAsync(
                request.IncidentId,
                request.Title,
                request.ActionType,
                request.RiskLevel,
                request.RequiresApproval,
                request.LinkedRunbookId,
                request.Steps,
                cancellationToken);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta da criação do workflow de mitigação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        MitigationWorkflowStatus Status,
        DateTimeOffset CreatedAt);
}
