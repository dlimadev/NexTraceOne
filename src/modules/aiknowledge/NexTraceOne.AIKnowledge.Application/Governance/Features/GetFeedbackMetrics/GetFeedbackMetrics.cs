using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetFeedbackMetrics;

/// <summary>
/// Feature: GetFeedbackMetrics — calcula métricas agregadas de satisfação de IA.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetFeedbackMetrics
{
    /// <summary>Query de métricas de feedback, com filtro opcional por agente.</summary>
    public sealed record Query(string? AgentName) : IQuery<Response>;

    /// <summary>Validador da query GetFeedbackMetrics.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.AgentName).MaximumLength(200).When(x => x.AgentName is not null);
        }
    }

    /// <summary>Handler que calcula métricas agregadas de feedback de IA.</summary>
    public sealed class Handler(
        IAiFeedbackRepository feedbackRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var positiveCount = await feedbackRepository.CountByRatingAsync(FeedbackRating.Positive, cancellationToken);
            var negativeCount = await feedbackRepository.CountByRatingAsync(FeedbackRating.Negative, cancellationToken);
            var neutralCount = await feedbackRepository.CountByRatingAsync(FeedbackRating.Neutral, cancellationToken);

            var totalFeedbacks = positiveCount + negativeCount + neutralCount;

            var satisfactionRate = (positiveCount + negativeCount) > 0
                ? Math.Round((decimal)positiveCount / (positiveCount + negativeCount) * 100, 2)
                : 0m;

            return new Response(totalFeedbacks, positiveCount, negativeCount, neutralCount, satisfactionRate);
        }
    }

    /// <summary>Resposta com métricas agregadas de feedback.</summary>
    public sealed record Response(
        int TotalFeedbacks,
        int PositiveCount,
        int NegativeCount,
        int NeutralCount,
        decimal SatisfactionRate);
}
