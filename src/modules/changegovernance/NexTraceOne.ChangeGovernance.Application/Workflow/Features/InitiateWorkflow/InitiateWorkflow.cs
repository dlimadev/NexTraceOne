using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Workflow.Application.Abstractions;
using NexTraceOne.Workflow.Domain.Entities;
using NexTraceOne.Workflow.Domain.Enums;
using NexTraceOne.Workflow.Domain.Errors;

namespace NexTraceOne.Workflow.Application.Features.InitiateWorkflow;

/// <summary>
/// Feature: InitiateWorkflow — inicia uma instância de workflow para uma release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class InitiateWorkflow
{
    /// <summary>Dados de um estágio a ser criado junto com a instância.</summary>
    public sealed record StageInput(string StageName, int RequiredApprovers, bool CommentRequired);

    /// <summary>Comando para iniciar uma instância de workflow vinculada a uma release.</summary>
    public sealed record Command(
        Guid WorkflowTemplateId,
        Guid ReleaseId,
        string SubmittedBy,
        IReadOnlyList<StageInput> Stages) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de inicialização de workflow.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowTemplateId).NotEmpty();
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.SubmittedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Stages).NotEmpty();
            RuleForEach(x => x.Stages).ChildRules(stage =>
            {
                stage.RuleFor(s => s.StageName).NotEmpty().MaximumLength(200);
                stage.RuleFor(s => s.RequiredApprovers).GreaterThanOrEqualTo(1);
            });
        }
    }

    /// <summary>Handler que cria a instância de workflow e seus estágios.</summary>
    public sealed class Handler(
        IWorkflowTemplateRepository templateRepository,
        IWorkflowInstanceRepository instanceRepository,
        IWorkflowStageRepository stageRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var template = await templateRepository.GetByIdAsync(
                WorkflowTemplateId.From(request.WorkflowTemplateId), cancellationToken);

            if (template is null)
                return WorkflowErrors.TemplateNotFound(request.WorkflowTemplateId.ToString());

            var instance = WorkflowInstance.Create(
                template.Id,
                request.ReleaseId,
                request.SubmittedBy,
                dateTimeProvider.UtcNow);

            instanceRepository.Add(instance);

            var stagesCreated = 0;
            foreach (var (stageInput, index) in request.Stages.Select((s, i) => (s, i)))
            {
                var stage = WorkflowStage.Create(
                    instance.Id,
                    stageInput.StageName,
                    index,
                    stageInput.RequiredApprovers,
                    stageInput.CommentRequired,
                    slaDurationHours: null);

                stageRepository.Add(stage);
                stagesCreated++;
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                instance.Id.Value,
                instance.Status.ToString(),
                stagesCreated);
        }
    }

    /// <summary>Resposta da inicialização da instância de workflow.</summary>
    public sealed record Response(Guid WorkflowInstanceId, string Status, int StagesCreated);
}
