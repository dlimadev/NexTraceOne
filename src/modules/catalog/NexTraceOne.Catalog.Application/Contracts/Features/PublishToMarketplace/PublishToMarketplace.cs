using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.PublishToMarketplace;

/// <summary>
/// Feature: PublishToMarketplace — publica um contrato no marketplace interno,
/// criando a listagem com metadados de descoberta e classificação.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class PublishToMarketplace
{
    /// <summary>Comando para publicar um contrato no marketplace interno.</summary>
    public sealed record Command(
        string ContractId,
        string Category,
        string? Tags,
        bool IsPromoted,
        string? Description,
        MarketplaceListingStatus Status,
        string? PublishedBy,
        string? TenantId = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de publicação no marketplace.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.Description).MaximumLength(4000).When(x => x.Description is not null);
            RuleFor(x => x.PublishedBy).MaximumLength(200).When(x => x.PublishedBy is not null);
        }
    }

    /// <summary>
    /// Handler que cria e persiste uma listagem de contrato no marketplace interno.
    /// Delega a criação ao factory method ContractListing.Publish.
    /// </summary>
    public sealed class Handler(
        IContractListingRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var listing = ContractListing.Publish(
                request.ContractId,
                request.Category,
                request.Tags,
                request.IsPromoted,
                request.Description,
                request.Status,
                request.PublishedBy,
                dateTimeProvider.UtcNow,
                request.TenantId);

            await repository.AddAsync(listing, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                listing.Id.Value,
                listing.ContractId,
                listing.Category,
                listing.Status,
                listing.IsPromoted,
                listing.PublishedAt);
        }
    }

    /// <summary>Resposta da publicação de contrato no marketplace.</summary>
    public sealed record Response(
        Guid ListingId,
        string ContractId,
        string Category,
        MarketplaceListingStatus Status,
        bool IsPromoted,
        DateTimeOffset PublishedAt);
}
