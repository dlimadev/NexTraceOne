using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.SubmitContractReview;

/// <summary>
/// Feature: SubmitContractReview — submete uma avaliação de contrato no marketplace interno.
/// Persiste rating, comentário e autoria para alimentar métricas de reputação.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class SubmitContractReview
{
    /// <summary>Comando para submeter uma avaliação de contrato no marketplace.</summary>
    public sealed record Command(
        Guid ListingId,
        string AuthorId,
        int Rating,
        string? Comment,
        string? TenantId = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de submissão de avaliação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ListingId).NotEmpty();
            RuleFor(x => x.AuthorId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Rating).InclusiveBetween(1, 5);
            RuleFor(x => x.Comment).MaximumLength(2000).When(x => x.Comment is not null);
        }
    }

    /// <summary>
    /// Handler que cria e persiste uma avaliação de contrato no marketplace.
    /// Delega a criação ao factory method MarketplaceReview.Submit.
    /// </summary>
    public sealed class Handler(
        IMarketplaceReviewRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var review = MarketplaceReview.Submit(
                ContractListingId.From(request.ListingId),
                request.AuthorId,
                request.Rating,
                request.Comment,
                dateTimeProvider.UtcNow,
                request.TenantId);

            await repository.AddAsync(review, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                review.Id.Value,
                review.ListingId.Value,
                review.AuthorId,
                review.Rating,
                review.ReviewedAt);
        }
    }

    /// <summary>Resposta da submissão de avaliação no marketplace.</summary>
    public sealed record Response(
        Guid ReviewId,
        Guid ListingId,
        string AuthorId,
        int Rating,
        DateTimeOffset ReviewedAt);
}
