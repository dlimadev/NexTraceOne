using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.RegisterConsumerExpectation;

/// <summary>
/// Feature: RegisterConsumerExpectation — regista a expectativa de um serviço consumidor
/// sobre um contrato publicado, como base para Consumer-Driven Contract Testing (CDCT).
/// O consumidor declara quais endpoints, campos e comportamentos espera do provider.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class RegisterConsumerExpectation
{
    /// <summary>Command de registo de expectativa de consumidor.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string ConsumerServiceName,
        string ConsumerDomain,
        string ExpectedSubsetJson,
        string? Notes) : ICommand<Response>;

    /// <summary>Valida a entrada do command.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ConsumerServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ConsumerDomain).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ExpectedSubsetJson).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que cria ou actualiza a expectativa do consumidor para o contrato.
    /// Se já existir uma expectativa do mesmo serviço para este contrato, actualiza-a.
    /// </summary>
    public sealed class Handler(
        IConsumerExpectationRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Verifica se já existe expectativa deste consumidor para este contrato
            var existing = await repository.GetByApiAssetAndConsumerAsync(
                request.ApiAssetId, request.ConsumerServiceName, cancellationToken);

            ConsumerExpectation expectation;
            bool isNew;

            if (existing is not null)
            {
                existing.Update(request.ExpectedSubsetJson, request.Notes);
                expectation = existing;
                isNew = false;
            }
            else
            {
                expectation = ConsumerExpectation.Create(
                    request.ApiAssetId,
                    request.ConsumerServiceName,
                    request.ConsumerDomain,
                    request.ExpectedSubsetJson,
                    request.Notes,
                    dateTimeProvider.UtcNow);
                repository.Add(expectation);
                isNew = true;
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                expectation.Id.Value,
                expectation.ApiAssetId,
                expectation.ConsumerServiceName,
                expectation.ConsumerDomain,
                expectation.RegisteredAt,
                isNew);
        }
    }

    /// <summary>Resposta do registo de expectativa de consumidor.</summary>
    public sealed record Response(
        Guid ExpectationId,
        Guid ApiAssetId,
        string ConsumerServiceName,
        string ConsumerDomain,
        DateTimeOffset RegisteredAt,
        bool IsNew);
}
