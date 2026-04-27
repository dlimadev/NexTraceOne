using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiEvalReport;

/// <summary>
/// Feature: GetAiEvalReport — relatório de comparação de modelos IA por dataset.
/// Retorna todas as runs de avaliação de um dataset, agrupadas por modelo,
/// permitindo comparação histórica de qualidade.
/// CC-05: AI Evaluation Harness.
/// </summary>
public static class GetAiEvalReport
{
    public sealed record Query(
        string TenantId,
        Guid DatasetId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.DatasetId).NotEmpty();
        }
    }

    public sealed class Handler(
        IAiEvalRunRepository runRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var runs = await runRepository.ListByDatasetAsync(request.DatasetId, request.TenantId, cancellationToken);

            var byModel = runs
                .Where(r => r.Status == NexTraceOne.AIKnowledge.Domain.Governance.Entities.AiEvalRunStatus.Completed)
                .GroupBy(r => r.ModelId)
                .Select(g =>
                {
                    var latest = g.OrderByDescending(r => r.StartedAt).First();
                    return new ModelSummary(
                        g.Key,
                        g.Count(),
                        latest.ExactMatchCount,
                        latest.CasesProcessed > 0
                            ? Math.Round((decimal)latest.ExactMatchCount / latest.CasesProcessed, 4)
                            : 0m,
                        latest.AverageSemanticSimilarity,
                        latest.ToolCallAccuracy,
                        latest.LatencyP50Ms,
                        latest.LatencyP95Ms,
                        latest.TotalTokenCost,
                        latest.StartedAt);
                })
                .OrderByDescending(m => m.LatestSemanticSimilarity)
                .ToList();

            return new Response(request.DatasetId, runs.Count, byModel);
        }
    }

    public sealed record ModelSummary(
        string ModelId,
        int TotalRuns,
        int LatestExactMatchCount,
        decimal LatestExactMatchRate,
        decimal LatestSemanticSimilarity,
        decimal LatestToolCallAccuracy,
        double LatestLatencyP50Ms,
        double LatestLatencyP95Ms,
        long LatestTotalTokenCost,
        DateTimeOffset LastRunAt);

    public sealed record Response(
        Guid DatasetId,
        int TotalRuns,
        IReadOnlyList<ModelSummary> ByModel);
}
