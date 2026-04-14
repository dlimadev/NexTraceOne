using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.MapConsumerRelationship;

/// <summary>
/// Feature: MapConsumerRelationship — mapeia ou actualiza a relação de consumo de uma API.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class MapConsumerRelationship
{
    /// <summary>Comando de mapeamento de relação de consumo.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        string ConsumerName,
        string ConsumerKind,
        string ConsumerEnvironment,
        string SourceType,
        string ExternalReference,
        decimal ConfidenceScore) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de mapeamento.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ConsumerName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ConsumerKind).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ConsumerEnvironment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.SourceType).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ExternalReference).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ConfidenceScore).InclusiveBetween(0.01m, 1.0m);
        }
    }

    /// <summary>Handler que mapeia ou actualiza a relação de consumo no grafo.</summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IDateTimeProvider dateTimeProvider,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var apiAssetId = ApiAssetId.From(request.ApiAssetId);
            var apiAsset = await apiAssetRepository.GetByIdAsync(apiAssetId, cancellationToken);
            if (apiAsset is null)
            {
                return CatalogGraphErrors.ApiAssetNotFound(request.ApiAssetId);
            }

            var consumerAsset = ConsumerAsset.Create(request.ConsumerName, request.ConsumerKind, request.ConsumerEnvironment);
            var discoverySource = DiscoverySource.Create(request.SourceType, request.ExternalReference, dateTimeProvider.UtcNow, request.ConfidenceScore);
            var relationshipResult = apiAsset.MapConsumerRelationship(consumerAsset, discoverySource, dateTimeProvider.UtcNow);

            if (relationshipResult.IsFailure)
            {
                return relationshipResult.Error;
            }

            await unitOfWork.CommitAsync(cancellationToken);

            var relationship = relationshipResult.Value;
            return new Response(
                relationship.Id.Value,
                request.ApiAssetId,
                relationship.ConsumerName,
                relationship.SourceType,
                relationship.ConfidenceScore,
                relationship.FirstObservedAt,
                relationship.LastObservedAt);
        }
    }

    /// <summary>Resposta do mapeamento da relação de consumo.</summary>
    public sealed record Response(
        Guid RelationshipId,
        Guid ApiAssetId,
        string ConsumerName,
        string SourceType,
        decimal ConfidenceScore,
        DateTimeOffset FirstObservedAt,
        DateTimeOffset LastObservedAt);
}
