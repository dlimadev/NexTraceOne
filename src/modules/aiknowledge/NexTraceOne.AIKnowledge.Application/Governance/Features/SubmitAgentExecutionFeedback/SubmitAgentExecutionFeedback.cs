using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SubmitAgentExecutionFeedback;

/// <summary>
/// Feature: SubmitAgentExecutionFeedback — regista feedback de trajectória para Agent Lightning.
/// Garante unicidade de feedback por execução e persiste para exportação futura ao trainer RL.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class SubmitAgentExecutionFeedback
{
    /// <summary>Comando de submissão de feedback de trajectória.</summary>
    public sealed record Command(
        Guid ExecutionId,
        int Rating,
        string Outcome,
        string? Comment,
        string? ActualOutcome,
        bool WasCorrect,
        int? TimeToResolveMinutes,
        string SubmittedBy,
        Guid TenantId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de feedback de trajectória.</summary>
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

    /// <summary>Handler que valida e persiste o feedback de trajectória.</summary>
    public sealed class Handler(
        IAiAgentExecutionRepository executionRepository,
        IAiAgentTrajectoryFeedbackRepository feedbackRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var execution = await executionRepository.GetByIdAsync(
                AiAgentExecutionId.From(request.ExecutionId), cancellationToken);

            if (execution is null)
                return AiGovernanceErrors.AgentExecutionNotFound(request.ExecutionId.ToString());

            var alreadyExists = await feedbackRepository.ExistsByExecutionIdAsync(
                execution.Id, cancellationToken);

            if (alreadyExists)
                return AiGovernanceErrors.TrajectoryFeedbackAlreadyExists(request.ExecutionId.ToString());

            var feedback = AiAgentTrajectoryFeedback.Submit(
                executionId: execution.Id,
                rating: request.Rating,
                outcome: request.Outcome,
                comment: request.Comment,
                actualOutcome: request.ActualOutcome,
                wasCorrect: request.WasCorrect,
                timeToResolveMinutes: request.TimeToResolveMinutes,
                submittedBy: request.SubmittedBy,
                tenantId: request.TenantId,
                submittedAt: DateTimeOffset.UtcNow);

            feedbackRepository.Add(feedback);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                FeedbackId: feedback.Id.Value,
                ExecutionId: execution.Id.Value,
                Rating: feedback.Rating,
                WasCorrect: feedback.WasCorrect);
        }
    }

    /// <summary>Resposta do registo de feedback de trajectória.</summary>
    public sealed record Response(
        Guid FeedbackId,
        Guid ExecutionId,
        int Rating,
        bool WasCorrect);
}
