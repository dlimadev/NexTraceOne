using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.StartPostReleaseReview;

/// <summary>
/// Feature: StartPostReleaseReview — inicia a review automática pós-release.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class StartPostReleaseReview
{
    /// <summary>Comando para iniciar a review automática pós-release.</summary>
    public sealed record Command(Guid ReleaseId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de início de review pós-release.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que inicia uma review automática pós-release.
    /// A review progride por janelas progressivas de observação.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IPostReleaseReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var existing = await reviewRepository.GetByReleaseIdAsync(releaseId, cancellationToken);
            if (existing is not null)
                return ChangeIntelligenceErrors.PostReleaseReviewAlreadyExists(request.ReleaseId.ToString());

            var review = PostReleaseReview.Start(releaseId, dateTimeProvider.UtcNow);
            reviewRepository.Add(review);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                review.Id.Value,
                release.Id.Value,
                review.CurrentPhase.ToString(),
                review.Outcome.ToString(),
                review.StartedAt);
        }
    }

    /// <summary>Resposta do início da review pós-release.</summary>
    public sealed record Response(
        Guid ReviewId,
        Guid ReleaseId,
        string CurrentPhase,
        string Outcome,
        DateTimeOffset StartedAt);
}
