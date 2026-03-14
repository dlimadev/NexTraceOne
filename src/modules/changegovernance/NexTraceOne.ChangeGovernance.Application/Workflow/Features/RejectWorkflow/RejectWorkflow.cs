using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Workflow.Application.Abstractions;
using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Enums;
using NexTraceOne.Workflow.Domain.Errors;

namespace NexTraceOne.Workflow.Application.Features.RejectWorkflow;

/// <summary>
/// Feature: RejectWorkflow — rejeita um workflow a partir de um estágio.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RejectWorkflow
{
    /// <summary>Comando para rejeitar um workflow. Comentário é obrigatório.</summary>
    public sealed record Command(
        Guid InstanceId,
        Guid StageId,
        string DecidedBy,
        string Comment) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de rejeição de workflow.</summary>
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

    /// <summary>Handler que registra a rejeição do estágio e finaliza o workflow como rejeitado.</summary>
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

            var rejectionResult = stage.RecordRejection(now);
            if (rejectionResult.IsFailure)
                return rejectionResult.Error;

            var decisionResult = ApprovalDecision.Create(
                stage.Id,
                instance.Id,
                request.DecidedBy,
                ApprovalAction.Rejected,
                request.Comment,
                now);

            if (decisionResult.IsFailure)
                return decisionResult.Error;

            decisionRepository.Add(decisionResult.Value);
            stageRepository.Update(stage);

            var completeResult = instance.Complete(WorkflowStatus.Rejected, now);
            if (completeResult.IsFailure)
                return completeResult.Error;

            instanceRepository.Update(instance);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                instance.Id.Value,
                instance.Status.ToString(),
                decisionResult.Value.Id.Value);
        }
    }

    /// <summary>Resposta da rejeição de um workflow.</summary>
    public sealed record Response(
        Guid InstanceId,
        string WorkflowStatus,
        Guid DecisionId);
}
