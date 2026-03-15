using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
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

    /// <summary>Handler que cria o workflow de mitigação com dados simulados.</summary>
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

            var response = new Response(
                WorkflowId: Guid.NewGuid(),
                Status: MitigationWorkflowStatus.Draft,
                CreatedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta da criação do workflow de mitigação.</summary>
    public sealed record Response(
        Guid WorkflowId,
        MitigationWorkflowStatus Status,
        DateTimeOffset CreatedAt);
}
