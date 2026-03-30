using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitEvaluation;

/// <summary>
/// Feature: SubmitEvaluation — regista uma nova avaliação de qualidade de IA.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class SubmitEvaluation
{
    /// <summary>Comando de submissão de uma avaliação de qualidade.</summary>
    public sealed record Command(
        string EvaluationType,
        Guid? ConversationId,
        Guid? MessageId,
        Guid? AgentExecutionId,
        string UserId,
        Guid TenantId,
        string ModelName,
        string? PromptTemplateName,
        decimal RelevanceScore,
        decimal AccuracyScore,
        decimal UsefulnessScore,
        decimal SafetyScore,
        decimal OverallScore,
        string? Feedback,
        string? Tags) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de submissão de avaliação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EvaluationType).NotEmpty()
                .Must(t => t is "automatic" or "user_feedback" or "peer_review")
                .WithMessage("EvaluationType must be 'automatic', 'user_feedback', or 'peer_review'.");
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.ModelName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RelevanceScore).InclusiveBetween(0m, 1m);
            RuleFor(x => x.AccuracyScore).InclusiveBetween(0m, 1m);
            RuleFor(x => x.UsefulnessScore).InclusiveBetween(0m, 1m);
            RuleFor(x => x.SafetyScore).InclusiveBetween(0m, 1m);
            RuleFor(x => x.OverallScore).InclusiveBetween(0m, 1m);
        }
    }

    /// <summary>Handler que regista uma nova avaliação de qualidade.</summary>
    public sealed class Handler(
        IAiEvaluationRepository evaluationRepository,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var evaluation = AiEvaluation.Create(
                evaluationType: request.EvaluationType,
                conversationId: request.ConversationId,
                messageId: request.MessageId,
                agentExecutionId: request.AgentExecutionId,
                userId: request.UserId,
                tenantId: request.TenantId,
                modelName: request.ModelName,
                promptTemplateName: request.PromptTemplateName,
                relevanceScore: request.RelevanceScore,
                accuracyScore: request.AccuracyScore,
                usefulnessScore: request.UsefulnessScore,
                safetyScore: request.SafetyScore,
                overallScore: request.OverallScore,
                feedback: request.Feedback,
                tags: request.Tags,
                evaluatedAt: dateTimeProvider.UtcNow);

            await evaluationRepository.AddAsync(evaluation, cancellationToken);

            return new Response(evaluation.Id.Value);
        }
    }

    /// <summary>Resposta da submissão da avaliação.</summary>
    public sealed record Response(Guid EvaluationId);
}
