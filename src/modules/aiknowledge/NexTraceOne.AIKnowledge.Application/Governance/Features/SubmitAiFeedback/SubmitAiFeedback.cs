using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitAiFeedback;

/// <summary>
/// Feature: SubmitAiFeedback — regista feedback do utilizador sobre uma interação de IA.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class SubmitAiFeedback
{
    /// <summary>Comando de submissão de feedback de IA.</summary>
    public sealed record Command(
        Guid? ConversationId,
        Guid? MessageId,
        Guid? AgentExecutionId,
        string RatingValue,
        string? Comment,
        string AgentName,
        string ModelUsed,
        string? QueryCategory) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de submissão de feedback.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.AgentName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ModelUsed).NotEmpty().MaximumLength(300);
            RuleFor(x => x.RatingValue).NotEmpty()
                .Must(v => v is "Positive" or "Negative" or "Neutral")
                .WithMessage("RatingValue must be 'Positive', 'Negative', or 'Neutral'.");
            RuleFor(x => x.Comment).MaximumLength(5000).When(x => x.Comment is not null);
            RuleFor(x => x.QueryCategory).MaximumLength(200).When(x => x.QueryCategory is not null);
        }
    }

    /// <summary>Handler que regista um novo feedback de IA.</summary>
    public sealed class Handler(
        IAiFeedbackRepository feedbackRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var rating = Enum.Parse<FeedbackRating>(request.RatingValue);

            var feedback = AiFeedback.Create(
                conversationId: request.ConversationId,
                messageId: request.MessageId,
                agentExecutionId: request.AgentExecutionId,
                rating: rating,
                comment: request.Comment,
                agentName: request.AgentName,
                modelUsed: request.ModelUsed,
                queryCategory: request.QueryCategory,
                createdByUserId: currentUser.Id,
                tenantId: currentTenant.Id,
                submittedAt: dateTimeProvider.UtcNow);

            await feedbackRepository.AddAsync(feedback, cancellationToken);

            return new Response(feedback.Id.Value);
        }
    }

    /// <summary>Resposta da submissão do feedback.</summary>
    public sealed record Response(Guid FeedbackId);
}
