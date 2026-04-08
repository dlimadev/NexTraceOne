using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListNegativeFeedback;

/// <summary>
/// Feature: ListNegativeFeedback — lista feedbacks negativos para análise e melhoria.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ListNegativeFeedback
{
    /// <summary>Query de listagem de feedbacks negativos com limite configurável.</summary>
    public sealed record Query(int Limit = 50) : IQuery<Response>;

    /// <summary>Validador da query ListNegativeFeedback.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Limit).InclusiveBetween(1, 500);
        }
    }

    /// <summary>Handler que lista feedbacks negativos.</summary>
    public sealed class Handler(
        IAiFeedbackRepository feedbackRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var negatives = await feedbackRepository.ListByRatingAsync(
                FeedbackRating.Negative, request.Limit, cancellationToken);

            var items = negatives
                .Select(f => new FeedbackItem(
                    f.Id.Value,
                    f.ConversationId,
                    f.MessageId,
                    f.AgentExecutionId,
                    f.Rating,
                    f.Comment,
                    f.AgentName,
                    f.ModelUsed,
                    f.QueryCategory,
                    f.CreatedByUserId,
                    f.SubmittedAt))
                .ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta da listagem de feedbacks negativos.</summary>
    public sealed record Response(IReadOnlyList<FeedbackItem> Items);

    /// <summary>Item resumido de um feedback negativo.</summary>
    public sealed record FeedbackItem(
        Guid FeedbackId,
        Guid? ConversationId,
        Guid? MessageId,
        Guid? AgentExecutionId,
        FeedbackRating Rating,
        string? Comment,
        string AgentName,
        string ModelUsed,
        string? QueryCategory,
        string CreatedByUserId,
        DateTimeOffset SubmittedAt);
}
