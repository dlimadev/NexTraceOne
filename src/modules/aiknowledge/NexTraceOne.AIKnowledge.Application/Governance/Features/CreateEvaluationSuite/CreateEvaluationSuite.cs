using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateEvaluationSuite;

/// <summary>
/// Feature: CreateEvaluationSuite — cria uma nova suite de avaliação do AI Evaluation Harness.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CreateEvaluationSuite
{
    /// <summary>Comando de criação de suite de avaliação.</summary>
    public sealed record Command(
        string Name,
        string DisplayName,
        string Description,
        string UseCase,
        string Version,
        Guid TenantId,
        Guid? TargetModelId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.UseCase).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    /// <summary>Handler que cria e persiste a suite de avaliação.</summary>
    public sealed class Handler(
        IEvaluationSuiteRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var suite = EvaluationSuite.Create(
                request.Name,
                request.DisplayName,
                request.Description,
                request.UseCase,
                request.Version,
                request.TenantId,
                request.TargetModelId);

            repository.Add(suite);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(suite.Id.Value, suite.Name, suite.Status.ToString(), dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta com os dados da suite criada.</summary>
    public sealed record Response(Guid SuiteId, string Name, string Status, DateTimeOffset CreatedAt);
}
