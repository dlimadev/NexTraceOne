using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateEvaluationDataset;

/// <summary>
/// Feature: CreateEvaluationDataset — cria um novo dataset de avaliação reutilizável.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class CreateEvaluationDataset
{
    /// <summary>Comando de criação de dataset de avaliação.</summary>
    public sealed record Command(
        string Name,
        string Description,
        string UseCase,
        string SourceType,
        Guid TenantId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidSourceTypes =
            ["Curated", "Generated", "Synthetic"];

        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
            RuleFor(x => x.UseCase).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.SourceType)
                .NotEmpty()
                .Must(s => ValidSourceTypes.Contains(s, StringComparer.OrdinalIgnoreCase))
                .WithMessage("SourceType must be one of: Curated, Generated, Synthetic.");
        }
    }

    /// <summary>Handler que cria e persiste o dataset de avaliação.</summary>
    public sealed class Handler(
        IEvaluationDatasetRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var sourceType = Enum.Parse<EvaluationDatasetSourceType>(request.SourceType, ignoreCase: true);

            var dataset = EvaluationDataset.Create(
                request.Name,
                request.Description,
                request.UseCase,
                sourceType,
                request.TenantId);

            repository.Add(dataset);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(dataset.Id.Value, dataset.Name, dataset.UseCase, dataset.SourceType.ToString());
        }
    }

    /// <summary>Resposta com os dados do dataset criado.</summary>
    public sealed record Response(Guid DatasetId, string Name, string UseCase, string SourceType);
}
