using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Workflow.Application.Abstractions;
using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Enums;
using NexTraceOne.Workflow.Domain.Errors;

namespace NexTraceOne.Workflow.Application.Features.AddObservation;

/// <summary>
/// Feature: AddObservation — adiciona uma observação informativa a um estágio sem impacto no fluxo.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class AddObservation
{
    /// <summary>Comando para adicionar uma observação a um estágio de workflow.</summary>
    public sealed record Command(
        Guid InstanceId,
        Guid StageId,
        string DecidedBy,
        string Comment) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de observação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.InstanceId).NotEmpty();
            RuleFor(x => x.StageId).NotEmpty();
            RuleFor(x => x.DecidedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000);
        }
    }

    /// <summary>Handler que registra a observação sem alterar o status do workflow ou estágio.</summary>
    public sealed class Handler(
        IWorkflowInstanceRepository instanceRepository,
        IWorkflowStageRepository stageRepository,
        IApprovalDecisionRepository decisionRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var instance = await instanceRepository.GetByIdAsync(
                WorkflowInstanceId.From(request.InstanceId), cancellationToken);

            if (instance is null)
                return WorkflowErrors.InstanceNotFound(request.InstanceId.ToString());

            var stage = await stageRepository.GetByIdAsync(
                WorkflowStageId.From(request.StageId), cancellationToken);

            if (stage is null)
                return WorkflowErrors.StageNotFound(request.StageId.ToString());

            var now = dateTimeProvider.UtcNow;

            var decisionResult = ApprovalDecision.Create(
                stage.Id,
                instance.Id,
                request.DecidedBy,
                ApprovalAction.Observation,
                request.Comment,
                now);

            if (decisionResult.IsFailure)
                return decisionResult.Error;

            decisionRepository.Add(decisionResult.Value);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                decisionResult.Value.Id.Value,
                stage.Id.Value);
        }
    }

    /// <summary>Resposta da adição de observação.</summary>
    public sealed record Response(
        Guid DecisionId,
        Guid StageId);
}
