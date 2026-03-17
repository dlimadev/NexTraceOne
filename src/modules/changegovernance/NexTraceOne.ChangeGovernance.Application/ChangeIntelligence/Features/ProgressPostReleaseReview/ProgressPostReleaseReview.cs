using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ProgressPostReleaseReview;

/// <summary>
/// Feature: ProgressPostReleaseReview — progride a review pós-release para a próxima fase.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ProgressPostReleaseReview
{
    /// <summary>Comando para progredir a review pós-release para a próxima fase.</summary>
    public sealed record Command(
        Guid ReleaseId,
        ObservationPhase NewPhase,
        ReviewOutcome Outcome,
        decimal ConfidenceScore,
        string Summary) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de progressão de review.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.NewPhase).IsInEnum();
            RuleFor(x => x.Outcome).IsInEnum();
            RuleFor(x => x.ConfidenceScore).InclusiveBetween(0m, 1m);
            RuleFor(x => x.Summary).NotEmpty().MaximumLength(2000);
        }
    }

    /// <summary>
    /// Handler que progride a review pós-release para a próxima fase de observação.
    /// Cada progressão compara indicadores observados com o baseline.
    /// </summary>
    public sealed class Handler(
        IPostReleaseReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var review = await reviewRepository.GetByReleaseIdAsync(releaseId, cancellationToken);

            if (review is null)
                return ChangeIntelligenceErrors.PostReleaseReviewNotFound(request.ReleaseId.ToString());

            var result = review.Progress(
                request.NewPhase,
                request.Outcome,
                request.ConfidenceScore,
                request.Summary,
                dateTimeProvider.UtcNow);

            if (result.IsFailure)
                return result.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                review.Id.Value,
                review.CurrentPhase.ToString(),
                review.Outcome.ToString(),
                review.ConfidenceScore,
                review.IsCompleted);
        }
    }

    /// <summary>Resposta da progressão da review pós-release.</summary>
    public sealed record Response(
        Guid ReviewId,
        string CurrentPhase,
        string Outcome,
        decimal ConfidenceScore,
        bool IsCompleted);
}
