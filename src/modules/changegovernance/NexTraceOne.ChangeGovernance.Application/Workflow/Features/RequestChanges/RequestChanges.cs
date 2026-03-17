using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Enums;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Errors;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.RequestChanges;

/// <summary>
/// Feature: RequestChanges — solicita alterações em um estágio de workflow.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RequestChanges
{
    /// <summary>Comando para solicitar alterações em um estágio. Comentário e itens são obrigatórios.</summary>
    public sealed record Command(
        Guid InstanceId,
        Guid StageId,
        string DecidedBy,
        string Comment,
        IReadOnlyList<string> Items) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de solicitação de alterações.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.InstanceId).NotEmpty();
            RuleFor(x => x.StageId).NotEmpty();
            RuleFor(x => x.DecidedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Items).NotEmpty();
            RuleForEach(x => x.Items).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Handler que registra a solicitação de alterações no estágio.</summary>
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

            var commentWithItems = $"{request.Comment}\n\nRequested changes:\n{string.Join("\n", request.Items.Select(i => $"- {i}"))}";

            var decisionResult = ApprovalDecision.Create(
                stage.Id,
                instance.Id,
                request.DecidedBy,
                ApprovalAction.RequestedChanges,
                commentWithItems,
                now);

            if (decisionResult.IsFailure)
                return decisionResult.Error;

            decisionRepository.Add(decisionResult.Value);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                decisionResult.Value.Id.Value,
                stage.Id.Value,
                request.Items.Count);
        }
    }

    /// <summary>Resposta da solicitação de alterações.</summary>
    public sealed record Response(
        Guid DecisionId,
        Guid StageId,
        int RequestedItemsCount);
}
