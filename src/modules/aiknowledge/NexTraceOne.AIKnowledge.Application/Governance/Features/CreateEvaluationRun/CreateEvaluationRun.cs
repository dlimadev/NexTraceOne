using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateEvaluationRun;

/// <summary>
/// Feature: CreateEvaluationRun — inicia uma nova execução de avaliação para uma suite.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CreateEvaluationRun
{
    /// <summary>Comando de criação de execução de avaliação.</summary>
    public sealed record Command(
        Guid SuiteId,
        Guid ModelId,
        string PromptVersion,
        Guid TenantId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SuiteId).NotEmpty();
            RuleFor(x => x.ModelId).NotEmpty();
            RuleFor(x => x.PromptVersion).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que cria e persiste a execução de avaliação.</summary>
    public sealed class Handler(
        IEvaluationSuiteRepository suiteRepository,
        IEvaluationRunRepository runRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var suite = await suiteRepository.GetByIdAsync(EvaluationSuiteId.From(request.SuiteId), cancellationToken);
            if (suite is null)
                return AiGovernanceErrors.EvaluationSuiteNotFound(request.SuiteId.ToString());

            var run = EvaluationRun.Create(
                suite.Id,
                request.ModelId,
                request.PromptVersion,
                request.TenantId);

            runRepository.Add(run);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(run.Id.Value, suite.Id.Value, run.Status.ToString(), dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta com os dados da execução criada.</summary>
    public sealed record Response(Guid RunId, Guid SuiteId, string Status, DateTimeOffset CreatedAt);
}
