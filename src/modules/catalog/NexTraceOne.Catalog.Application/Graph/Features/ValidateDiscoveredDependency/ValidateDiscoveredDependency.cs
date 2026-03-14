using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.EngineeringGraph.Application.Abstractions;
using NexTraceOne.EngineeringGraph.Domain.Entities;
using NexTraceOne.EngineeringGraph.Domain.Errors;

namespace NexTraceOne.EngineeringGraph.Application.Features.ValidateDiscoveredDependency;

/// <summary>
/// Feature: ValidateDiscoveredDependency — valida confiança mínima de uma dependência descoberta.
/// </summary>
public static class ValidateDiscoveredDependency
{
    /// <summary>Query de validação de dependência descoberta.</summary>
    public sealed record Query(Guid ApiAssetId, Guid RelationshipId, decimal MinimumConfidence) : IQuery<Response>;

    /// <summary>Valida a entrada da query de validação.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.RelationshipId).NotEmpty();
            RuleFor(x => x.MinimumConfidence).InclusiveBetween(0.01m, 1.0m);
        }
    }

    /// <summary>Handler que valida se a dependência possui confiança suficiente.</summary>
    public sealed class Handler(IApiAssetRepository apiAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var apiAssetId = ApiAssetId.From(request.ApiAssetId);
            var apiAsset = await apiAssetRepository.GetByIdAsync(apiAssetId, cancellationToken);
            if (apiAsset is null)
            {
                return EngineeringGraphErrors.ApiAssetNotFound(request.ApiAssetId);
            }

            var relationshipId = ConsumerRelationshipId.From(request.RelationshipId);
            var validationResult = apiAsset.ValidateDiscoveredDependency(relationshipId, request.MinimumConfidence);
            if (validationResult.IsFailure)
            {
                return validationResult.Error;
            }

            var relationship = validationResult.Value;
            return new Response(relationship.Id.Value, relationship.ConsumerName, relationship.ConfidenceScore, true);
        }
    }

    /// <summary>Resposta da validação da dependência.</summary>
    public sealed record Response(Guid RelationshipId, string ConsumerName, decimal ConfidenceScore, bool IsValid);
}
