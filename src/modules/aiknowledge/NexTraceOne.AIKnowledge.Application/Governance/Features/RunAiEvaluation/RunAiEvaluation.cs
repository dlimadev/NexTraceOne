using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.RunAiEvaluation;

/// <summary>
/// Feature: RunAiEvaluation — executa avaliação de um dataset contra um modelo IA.
/// Simula execução dos casos de teste e calcula métricas agregadas:
/// ExactMatch, SemanticSimilarity, ToolCallAccuracy, LatencyP50/P95, TotalTokenCost.
/// CC-05: AI Evaluation Harness.
/// </summary>
public static class RunAiEvaluation
{
    public sealed record Command(
        string TenantId,
        Guid DatasetId,
        string ModelId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DatasetId).NotEmpty();
            RuleFor(x => x.ModelId).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class Handler(
        IAiEvalDatasetRepository datasetRepository,
        IAiEvalRunRepository runRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var dataset = await datasetRepository.GetByIdAsync(new AiEvalDatasetId(request.DatasetId), cancellationToken);
            if (dataset is null)
                return Result<Response>.NotFound($"Dataset '{request.DatasetId}' not found.");

            if (!dataset.IsActive)
                return Result<Response>.Failure("Dataset is not active.");

            var run = AiEvalRun.Create(request.TenantId, request.DatasetId, request.ModelId, clock.UtcNow);
            await runRepository.AddAsync(run, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            run.Start();

            var metrics = SimulateEvaluation(dataset.TestCasesJson, dataset.TestCaseCount);

            run.Complete(
                metrics.CasesProcessed,
                metrics.ExactMatchCount,
                metrics.AverageSemanticSimilarity,
                metrics.ToolCallAccuracy,
                metrics.LatencyP50Ms,
                metrics.LatencyP95Ms,
                metrics.TotalTokenCost,
                clock.UtcNow);

            await runRepository.UpdateAsync(run, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                run.Id.Value,
                run.ModelId,
                run.Status.ToString(),
                run.CasesProcessed,
                run.ExactMatchCount,
                run.AverageSemanticSimilarity,
                run.ToolCallAccuracy,
                run.LatencyP50Ms,
                run.LatencyP95Ms,
                run.TotalTokenCost,
                run.StartedAt,
                run.CompletedAt);
        }

        private static EvalMetrics SimulateEvaluation(string testCasesJson, int testCaseCount)
        {
            var caseCount = testCaseCount > 0 ? testCaseCount : ParseCaseCount(testCasesJson);
            if (caseCount == 0)
                return new EvalMetrics(0, 0, 0m, 0m, 0, 0, 0);

            // Deterministic simulation based on test case count for reproducibility
            var exactMatches = (int)(caseCount * 0.72);
            var avgSimilarity = 0.84m;
            var toolAccuracy = 0.91m;
            var p50 = 320.0;
            var p95 = 950.0;
            var totalTokens = (long)(caseCount * 1200);

            return new EvalMetrics(caseCount, exactMatches, avgSimilarity, toolAccuracy, p50, p95, totalTokens);
        }

        private static int ParseCaseCount(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.GetArrayLength() : 0;
            }
            catch
            {
                return 0;
            }
        }

        private sealed record EvalMetrics(
            int CasesProcessed,
            int ExactMatchCount,
            decimal AverageSemanticSimilarity,
            decimal ToolCallAccuracy,
            double LatencyP50Ms,
            double LatencyP95Ms,
            long TotalTokenCost);
    }

    public sealed record Response(
        Guid RunId,
        string ModelId,
        string Status,
        int CasesProcessed,
        int ExactMatchCount,
        decimal AverageSemanticSimilarity,
        decimal ToolCallAccuracy,
        double LatencyP50Ms,
        double LatencyP95Ms,
        long TotalTokenCost,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt);
}
