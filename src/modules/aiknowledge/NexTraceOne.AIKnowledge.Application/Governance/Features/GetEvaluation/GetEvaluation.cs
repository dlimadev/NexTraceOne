using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetEvaluation;

/// <summary>
/// Feature: GetEvaluation — obtém detalhes completos de uma avaliação de qualidade.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class GetEvaluation
{
    /// <summary>Query de consulta de uma avaliação de qualidade pelo identificador.</summary>
    public sealed record Query(Guid EvaluationId) : IQuery<Response>;

    /// <summary>Validador da query GetEvaluation.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.EvaluationId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém os detalhes completos de uma avaliação.</summary>
    public sealed class Handler(
        IAiEvaluationRepository evaluationRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var evaluation = await evaluationRepository.GetByIdAsync(
                AiEvaluationId.From(request.EvaluationId), cancellationToken);

            if (evaluation is null)
                return AiGovernanceErrors.EvaluationNotFound(request.EvaluationId.ToString());

            return new Response(
                evaluation.Id.Value, evaluation.EvaluationType,
                evaluation.ConversationId, evaluation.MessageId,
                evaluation.AgentExecutionId, evaluation.UserId,
                evaluation.TenantId, evaluation.ModelName,
                evaluation.PromptTemplateName, evaluation.RelevanceScore,
                evaluation.AccuracyScore, evaluation.UsefulnessScore,
                evaluation.SafetyScore, evaluation.OverallScore,
                evaluation.Feedback, evaluation.Tags, evaluation.EvaluatedAt);
        }
    }

    /// <summary>Resposta com detalhes completos de uma avaliação de qualidade.</summary>
    public sealed record Response(
        Guid EvaluationId, string EvaluationType, Guid? ConversationId,
        Guid? MessageId, Guid? AgentExecutionId, string UserId,
        Guid TenantId, string ModelName, string? PromptTemplateName,
        decimal RelevanceScore, decimal AccuracyScore, decimal UsefulnessScore,
        decimal SafetyScore, decimal OverallScore, string? Feedback,
        string? Tags, DateTimeOffset EvaluatedAt);
}
