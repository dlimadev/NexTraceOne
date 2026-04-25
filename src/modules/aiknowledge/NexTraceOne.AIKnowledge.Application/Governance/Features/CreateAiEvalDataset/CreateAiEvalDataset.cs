using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.CreateAiEvalDataset;

/// <summary>
/// Feature: CreateAiEvalDataset — cria um novo dataset de avaliação de modelos IA.
/// CC-05: AI Evaluation Harness.
/// </summary>
public static class CreateAiEvalDataset
{
    public sealed record Command(
        string TenantId,
        string Name,
        string UseCase,
        string? Description,
        string TestCasesJson,
        int TestCaseCount) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.UseCase).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TestCasesJson).NotEmpty();
            RuleFor(x => x.TestCaseCount).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class Handler(
        IAiEvalDatasetRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var dataset = AiEvalDataset.Create(
                request.TenantId,
                request.Name,
                request.UseCase,
                request.Description,
                request.TestCasesJson,
                request.TestCaseCount,
                clock.UtcNow);

            await repository.AddAsync(dataset, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(dataset.Id.Value, dataset.Name, dataset.UseCase, dataset.TestCaseCount, dataset.CreatedAt);
        }
    }

    public sealed record Response(Guid DatasetId, string Name, string UseCase, int TestCaseCount, DateTimeOffset CreatedAt);
}
