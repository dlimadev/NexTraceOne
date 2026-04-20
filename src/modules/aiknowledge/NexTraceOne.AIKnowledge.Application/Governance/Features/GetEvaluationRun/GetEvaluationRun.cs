using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetEvaluationRun;

/// <summary>
/// Feature: GetEvaluationRun — obtém detalhes de uma execução de avaliação.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetEvaluationRun
{
    /// <summary>Query de obtenção de execução de avaliação.</summary>
    public sealed record Query(Guid RunId) : IQuery<Response>;

    /// <summary>Handler que carrega e mapeia a execução de avaliação.</summary>
    public sealed class Handler(IEvaluationRunRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var run = await repository.GetByIdAsync(EvaluationRunId.From(request.RunId), cancellationToken);
            if (run is null)
                return AiGovernanceErrors.EvaluationRunNotFound(request.RunId.ToString());

            return new Response(
                run.Id.Value,
                run.SuiteId.Value,
                run.ModelId,
                run.PromptVersion,
                run.Status.ToString(),
                run.StartedAt,
                run.CompletedAt,
                run.TotalCases,
                run.PassedCases,
                run.FailedCases,
                run.AverageLatencyMs,
                run.TotalTokenCost);
        }
    }

    /// <summary>Resposta com detalhes completos da execução de avaliação.</summary>
    public sealed record Response(
        Guid RunId,
        Guid SuiteId,
        Guid ModelId,
        string PromptVersion,
        string Status,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        int TotalCases,
        int PassedCases,
        int FailedCases,
        double AverageLatencyMs,
        decimal TotalTokenCost);
}
