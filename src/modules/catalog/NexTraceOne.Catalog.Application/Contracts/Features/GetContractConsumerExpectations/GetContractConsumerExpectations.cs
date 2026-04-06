using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetContractConsumerExpectations;

/// <summary>
/// Feature: GetContractConsumerExpectations — lista as expectativas de consumidores
/// registadas para um contrato (ApiAssetId), para apoiar CDCT e governança de dependências.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetContractConsumerExpectations
{
    /// <summary>Query de listagem de expectativas de consumidores de um contrato.</summary>
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>Handler que lista as expectativas activas de consumidores para um contrato.</summary>
    public sealed class Handler(IConsumerExpectationRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var expectations = await repository.ListByApiAssetAsync(request.ApiAssetId, cancellationToken);

            var items = expectations
                .Where(e => e.IsActive)
                .Select(e => new ConsumerExpectationItem(
                    e.Id.Value,
                    e.ConsumerServiceName,
                    e.ConsumerDomain,
                    e.ExpectedSubsetJson,
                    e.Notes,
                    e.RegisteredAt))
                .ToList()
                .AsReadOnly();

            return new Response(request.ApiAssetId, items);
        }
    }

    /// <summary>Expectativa de consumidor resumida.</summary>
    public sealed record ConsumerExpectationItem(
        Guid ExpectationId,
        string ConsumerServiceName,
        string ConsumerDomain,
        string ExpectedSubsetJson,
        string Notes,
        DateTimeOffset RegisteredAt);

    /// <summary>Resposta com a lista de expectativas de consumidores do contrato.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        IReadOnlyList<ConsumerExpectationItem> Expectations);
}
