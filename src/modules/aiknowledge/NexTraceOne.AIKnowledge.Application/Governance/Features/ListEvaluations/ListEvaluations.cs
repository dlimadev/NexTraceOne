using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListEvaluations;

/// <summary>
/// Feature: ListEvaluations — lista avaliações de qualidade com filtros.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListEvaluations
{
    /// <summary>Query de listagem filtrada de avaliações de qualidade.</summary>
    public sealed record Query(
        Guid? ConversationId,
        Guid? AgentExecutionId,
        string? UserId) : IQuery<Response>;

    /// <summary>Handler que lista avaliações com filtros opcionais.</summary>
    public sealed class Handler(
        IAiEvaluationRepository evaluationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            IReadOnlyList<Domain.Governance.Entities.AiEvaluation> evaluations;

            if (request.ConversationId.HasValue)
                evaluations = await evaluationRepository.GetByConversationAsync(request.ConversationId.Value, cancellationToken);
            else if (request.AgentExecutionId.HasValue)
                evaluations = await evaluationRepository.GetByAgentExecutionAsync(request.AgentExecutionId.Value, cancellationToken);
            else if (request.UserId is not null)
                evaluations = await evaluationRepository.GetByUserAsync(request.UserId, cancellationToken);
            else
                evaluations = Array.Empty<Domain.Governance.Entities.AiEvaluation>();

            var items = evaluations
                .Select(e => new EvaluationItem(
                    e.Id.Value, e.EvaluationType, e.ConversationId,
                    e.MessageId, e.AgentExecutionId, e.UserId,
                    e.TenantId, e.ModelName, e.PromptTemplateName,
                    e.RelevanceScore, e.AccuracyScore, e.UsefulnessScore,
                    e.SafetyScore, e.OverallScore, e.Feedback,
                    e.Tags, e.EvaluatedAt))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de avaliações.</summary>
    public sealed record Response(
        IReadOnlyList<EvaluationItem> Items,
        int TotalCount);

    /// <summary>Item resumido de uma avaliação de qualidade.</summary>
    public sealed record EvaluationItem(
        Guid EvaluationId, string EvaluationType, Guid? ConversationId,
        Guid? MessageId, Guid? AgentExecutionId, string UserId,
        Guid TenantId, string ModelName, string? PromptTemplateName,
        decimal RelevanceScore, decimal AccuracyScore, decimal UsefulnessScore,
        decimal SafetyScore, decimal OverallScore, string? Feedback,
        string? Tags, DateTimeOffset EvaluatedAt);
}
