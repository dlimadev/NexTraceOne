using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Workflow.Application.Abstractions;
using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Enums;
using NexTraceOne.Workflow.Domain.Errors;

namespace NexTraceOne.Workflow.Application.Features.ApproveStage;

/// <summary>
/// Feature: ApproveStage — registra a aprovação de um estágio de workflow.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ApproveStage
{
    /// <summary>Comando para aprovar um estágio de workflow.</summary>
    public sealed record Command(
        Guid StageId,
        string DecidedBy,
        string? Comment) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de aprovação de estágio.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.StageId).NotEmpty();
            RuleFor(x => x.DecidedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Comment).MaximumLength(2000);
        }
    }

    /// <summary>Handler que registra a aprovação, avança o workflow se todos os estágios estiverem concluídos.</summary>
    public sealed class Handler(
        IWorkflowStageRepository stageRepository,
        IWorkflowInstanceRepository instanceRepository,
        IApprovalDecisionRepository decisionRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var stage = await stageRepository.GetByIdAsync(
                WorkflowStageId.From(request.StageId), cancellationToken);

            if (stage is null)
                return WorkflowErrors.StageNotFound(request.StageId.ToString());

            var instance = await instanceRepository.GetByIdAsync(
                stage.WorkflowInstanceId, cancellationToken);

            if (instance is null)
                return WorkflowErrors.InstanceNotFound(stage.WorkflowInstanceId.Value.ToString());

            if (instance.SubmittedBy == request.DecidedBy)
                return WorkflowErrors.CannotApproveOwnSubmission();

            var now = dateTimeProvider.UtcNow;

            var approvalResult = stage.RecordApproval(now);
            if (approvalResult.IsFailure)
                return approvalResult.Error;

            var decisionResult = ApprovalDecision.Create(
                stage.Id,
                instance.Id,
                request.DecidedBy,
                ApprovalAction.Approved,
                request.Comment,
                now);

            if (decisionResult.IsFailure)
                return decisionResult.Error;

            decisionRepository.Add(decisionResult.Value);
            stageRepository.Update(stage);

            if (stage.IsComplete)
            {
                var stages = await stageRepository.ListByInstanceIdAsync(
                    instance.Id, cancellationToken);

                var allComplete = stages.All(s => s.IsComplete);

                if (allComplete)
                {
                    var completeResult = instance.Complete(WorkflowStatus.Approved, now);
                    if (completeResult.IsFailure)
                        return completeResult.Error;
                }
                else
                {
                    var advanceResult = instance.Advance();
                    if (advanceResult.IsFailure)
                        return advanceResult.Error;
                }

                instanceRepository.Update(instance);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                decisionResult.Value.Id.Value,
                stage.Id.Value,
                stage.Status.ToString(),
                stage.IsComplete);
        }
    }

    /// <summary>Resposta da aprovação de um estágio de workflow.</summary>
    public sealed record Response(
        Guid DecisionId,
        Guid StageId,
        string StageStatus,
        bool StageCompleted);
}
