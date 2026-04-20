using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListEvaluationSuites;

/// <summary>
/// Feature: ListEvaluationSuites — lista suites de avaliação de um tenant com filtros opcionais.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListEvaluationSuites
{
    /// <summary>Query de listagem de suites de avaliação.</summary>
    public sealed record Query(Guid TenantId, string? UseCase, int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Handler que lista as suites com paginação.</summary>
    public sealed class Handler(IEvaluationSuiteRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var suites = await repository.ListByTenantAsync(
                request.TenantId, request.UseCase, request.Page, request.PageSize, cancellationToken);

            var total = await repository.CountByTenantAsync(request.TenantId, request.UseCase, cancellationToken);

            var items = suites.Select(s => new EvaluationSuiteSummaryDto(
                s.Id.Value,
                s.Name,
                s.UseCase,
                s.Status.ToString(),
                s.Version)).ToList();

            return new Response(items, total);
        }
    }

    /// <summary>Resumo de uma suite de avaliação para listagem.</summary>
    public sealed record EvaluationSuiteSummaryDto(
        Guid SuiteId,
        string Name,
        string UseCase,
        string Status,
        string Version);

    /// <summary>Resposta paginada de listagem de suites.</summary>
    public sealed record Response(IReadOnlyList<EvaluationSuiteSummaryDto> Items, int TotalCount);
}
