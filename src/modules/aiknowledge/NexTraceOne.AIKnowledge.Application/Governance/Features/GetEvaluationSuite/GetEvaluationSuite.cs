using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetEvaluationSuite;

/// <summary>
/// Feature: GetEvaluationSuite — obtém detalhes completos de uma suite de avaliação.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetEvaluationSuite
{
    /// <summary>Query de obtenção de suite de avaliação.</summary>
    public sealed record Query(Guid SuiteId) : IQuery<Response>;

    /// <summary>Handler que carrega e mapeia a suite de avaliação.</summary>
    public sealed class Handler(
        IEvaluationSuiteRepository suiteRepository,
        IEvaluationCaseRepository caseRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var suite = await suiteRepository.GetByIdAsync(EvaluationSuiteId.From(request.SuiteId), cancellationToken);
            if (suite is null)
                return AiGovernanceErrors.EvaluationSuiteNotFound(request.SuiteId.ToString());

            var cases = await caseRepository.ListBySuiteAsync(suite.Id, cancellationToken);

            return new Response(
                suite.Id.Value,
                suite.Name,
                suite.DisplayName,
                suite.Description,
                suite.UseCase,
                suite.TargetModelId,
                suite.Status.ToString(),
                suite.Version,
                cases.Count,
                suite.CreatedAt,
                suite.UpdatedAt);
        }
    }

    /// <summary>Resposta com detalhes completos da suite de avaliação.</summary>
    public sealed record Response(
        Guid SuiteId,
        string Name,
        string DisplayName,
        string Description,
        string UseCase,
        Guid? TargetModelId,
        string Status,
        string Version,
        int CaseCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
