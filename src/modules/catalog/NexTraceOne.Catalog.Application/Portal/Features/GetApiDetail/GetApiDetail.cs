using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetApiDetail;

/// <summary>
/// Feature: GetApiDetail — retorna detalhes completos de uma API incluindo sinais de confiança.
/// Agrega dados do Catalog Graph (ApiAsset + ServiceAsset), Contracts (ContractVersion)
/// e Portal (Subscriptions) para compor uma visão unificada da API.
/// </summary>
public static class GetApiDetail
{
    /// <summary>Query para obter detalhes de uma API por identificador.</summary>
    public sealed record Query(Guid ApiAssetId) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de detalhes de API.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que retorna detalhes enriquecidos de uma API.
    /// Agrega dados do Catalog Graph, Contracts e Portal Subscriptions.
    /// </summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IContractVersionRepository contractVersionRepository,
        ISubscriptionRepository subscriptionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Fetch the ApiAsset from Catalog Graph (includes OwnerService and ConsumerRelationships)
            var apiAsset = await apiAssetRepository.GetByIdAsync(
                ApiAssetId.From(request.ApiAssetId), cancellationToken);

            if (apiAsset is null)
            {
                return DeveloperPortalErrors.ApiNotFound(request.ApiAssetId.ToString());
            }

            // Fetch latest contract version for this API
            var latestContract = await contractVersionRepository.GetLatestByApiAssetAsync(
                request.ApiAssetId, cancellationToken);

            // Fetch subscriptions for consumer count
            var subscriptions = await subscriptionRepository.GetByApiAssetAsync(
                request.ApiAssetId, cancellationToken);

            // Get latest deployment info from contract if available
            DateTimeOffset? lastDeployment = null;

            // Build trust signals from contract data
            var isContractValid = latestContract is not null &&
                latestContract.LifecycleState is
                    Domain.Contracts.Enums.ContractLifecycleState.Approved or
                    Domain.Contracts.Enums.ContractLifecycleState.Locked;

            var isDeprecated = latestContract?.LifecycleState is
                Domain.Contracts.Enums.ContractLifecycleState.Deprecated or
                Domain.Contracts.Enums.ContractLifecycleState.Sunset or
                Domain.Contracts.Enums.ContractLifecycleState.Retired;

            var trust = new TrustSignals(
                Owner: apiAsset.OwnerService?.TechnicalOwner,
                Team: apiAsset.OwnerService?.TeamName,
                Status: latestContract?.LifecycleState.ToString() ?? "Unknown",
                LastUpdated: latestContract?.UpdatedAt,
                ContractVersion: latestContract?.SemVer,
                IsContractValid: isContractValid,
                PlaygroundEnabled: true,
                IsDeprecated: isDeprecated,
                DeprecationDate: latestContract?.DeprecationDate,
                RecommendedVersion: null,
                DocumentationCompleteness: latestContract?.LastOverallScore ?? 0m,
                OverallTrustScore: CalculateTrustScore(latestContract, apiAsset));

            return Result<Response>.Success(new Response(
                ApiAssetId: request.ApiAssetId,
                Name: apiAsset.Name,
                Description: null,
                RoutePattern: apiAsset.RoutePattern,
                Owner: apiAsset.OwnerService?.TechnicalOwner,
                Team: apiAsset.OwnerService?.TeamName,
                Status: latestContract?.LifecycleState.ToString() ?? "Unknown",
                CurrentVersion: latestContract?.SemVer,
                Environment: null,
                Trust: trust,
                ConsumerCount: apiAsset.ConsumerRelationships.Count,
                SubscriberCount: subscriptions.Count,
                LastDeployment: lastDeployment,
                Tags: []));
        }

        private const decimal ScoreHasContract = 30m;
        private const decimal ScoreValidState = 20m;
        private const decimal ScoreHasOwner = 15m;
        private const decimal ScoreHasTeam = 10m;
        private const decimal QualityScoreWeight = 0.25m;
        private const decimal MaxQualityContribution = 25m;
        private const decimal MaxTotalScore = 100m;

        private static decimal CalculateTrustScore(
            Domain.Contracts.Entities.ContractVersion? contract,
            ApiAsset apiAsset)
        {
            var score = 0m;

            // Has contract
            if (contract is not null) score += ScoreHasContract;

            // Contract is valid (Approved/Locked)
            if (contract?.LifecycleState is
                Domain.Contracts.Enums.ContractLifecycleState.Approved or
                Domain.Contracts.Enums.ContractLifecycleState.Locked)
                score += ScoreValidState;

            // Has owner
            if (!string.IsNullOrEmpty(apiAsset.OwnerService?.TechnicalOwner))
                score += ScoreHasOwner;

            // Has team
            if (!string.IsNullOrEmpty(apiAsset.OwnerService?.TeamName))
                score += ScoreHasTeam;

            // Contract quality score contribution
            if (contract?.LastOverallScore is not null)
                score += Math.Min(contract.LastOverallScore.Value * QualityScoreWeight, MaxQualityContribution);

            return Math.Min(score, MaxTotalScore);
        }
    }

    /// <summary>Sinais de confiança e qualidade da API.</summary>
    public sealed record TrustSignals(
        string? Owner,
        string? Team,
        string Status,
        DateTimeOffset? LastUpdated,
        string? ContractVersion,
        bool IsContractValid,
        bool PlaygroundEnabled,
        bool IsDeprecated,
        DateTimeOffset? DeprecationDate,
        string? RecommendedVersion,
        decimal DocumentationCompleteness,
        decimal OverallTrustScore);

    /// <summary>Resposta com detalhes completos da API incluindo sinais de confiança.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        string Name,
        string? Description,
        string? RoutePattern,
        string? Owner,
        string? Team,
        string Status,
        string? CurrentVersion,
        string? Environment,
        TrustSignals Trust,
        int ConsumerCount,
        int SubscriberCount,
        DateTimeOffset? LastDeployment,
        IReadOnlyList<string> Tags);
}
