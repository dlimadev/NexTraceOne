using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.CreateWorkflowTemplate;

/// <summary>
/// Feature: CreateWorkflowTemplate — cria um novo template reutilizável de workflow de aprovação.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateWorkflowTemplate
{
    /// <summary>Comando para criação de um template de workflow.</summary>
    public sealed record Command(
        string Name,
        string Description,
        string ChangeType,
        string ApiCriticality,
        string TargetEnvironment,
        int MinimumApprovers) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de template.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotNull().MaximumLength(2000);
            RuleFor(x => x.ChangeType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ApiCriticality).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TargetEnvironment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.MinimumApprovers).GreaterThanOrEqualTo(1);
        }
    }

    /// <summary>Handler que cria um novo WorkflowTemplate e o persiste.</summary>
    public sealed class Handler(
        IWorkflowTemplateRepository repository,
        IWorkflowUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var template = WorkflowTemplate.Create(
                request.Name,
                request.Description,
                request.ChangeType,
                request.ApiCriticality,
                request.TargetEnvironment,
                request.MinimumApprovers,
                dateTimeProvider.UtcNow);

            repository.Add(template);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(template.Id.Value, template.Name, template.IsActive);
        }
    }

    /// <summary>Resposta da criação do template de workflow.</summary>
    public sealed record Response(Guid TemplateId, string Name, bool IsActive);
}
