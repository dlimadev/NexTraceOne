using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.RateSkillExecution;

/// <summary>
/// Feature: RateSkillExecution — regista feedback sobre uma execução de skill.
/// Atualiza a classificação média da skill correspondente.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class RateSkillExecution
{
    /// <summary>Comando de submissão de feedback sobre uma execução.</summary>
    public sealed record Command(
        Guid ExecutionId,
        int Rating,
        string Outcome,
        string? Comment,
        string? ActualOutcome,
        bool WasCorrect,
        string SubmittedBy,
        Guid TenantId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de feedback.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ExecutionId).NotEmpty();
            RuleFor(x => x.Rating).InclusiveBetween(1, 5);
            RuleFor(x => x.Outcome).NotEmpty().MaximumLength(100);
            RuleFor(x => x.SubmittedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Comment).MaximumLength(2000);
            RuleFor(x => x.ActualOutcome).MaximumLength(2000);
        }
    }

    /// <summary>Handler que regista feedback e actualiza a classificação média da skill.</summary>
    public sealed class Handler(
        IAiSkillExecutionRepository executionRepository,
        IAiSkillRepository skillRepository,
        IAiSkillFeedbackRepository feedbackRepository) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var execution = await executionRepository.GetByIdAsync(
                AiSkillExecutionId.From(request.ExecutionId), cancellationToken);

            if (execution is null)
                return AiGovernanceErrors.SkillExecutionNotFound(request.ExecutionId.ToString());

            var feedback = AiSkillFeedback.Submit(
                skillExecutionId: execution.Id,
                rating: request.Rating,
                outcome: request.Outcome,
                comment: request.Comment,
                actualOutcome: request.ActualOutcome,
                wasCorrect: request.WasCorrect,
                submittedBy: request.SubmittedBy,
                tenantId: request.TenantId,
                submittedAt: DateTimeOffset.UtcNow);

            feedbackRepository.Add(feedback);

            // Atualiza a classificação média da skill
            var skill = await skillRepository.GetByIdAsync(execution.SkillId, cancellationToken);
            if (skill is not null)
            {
                skill.IncrementExecutionCount();
                skill.UpdateAverageRating(request.Rating);
            }

            var newAverage = await feedbackRepository.GetAverageRatingBySkillAsync(
                execution.SkillId, cancellationToken) ?? request.Rating;

            return new Response(
                FeedbackId: feedback.Id.Value,
                SkillId: execution.SkillId.Value,
                NewAverageRating: newAverage);
        }
    }

    /// <summary>Resposta do registo de feedback.</summary>
    public sealed record Response(Guid FeedbackId, Guid SkillId, double NewAverageRating);
}
