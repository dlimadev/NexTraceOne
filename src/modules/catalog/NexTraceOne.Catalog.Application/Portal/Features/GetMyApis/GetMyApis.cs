using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Abstractions;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetMyApis;

/// <summary>
/// Feature: GetMyApis — lista APIs de que o utilizador é owner ou responsável.
/// Consulta o Catalog Graph para encontrar ApiAssets cujo ServiceAsset pertence
/// ao owner indicado, e enriquece com dados de contratos e subscrições.
/// </summary>
public static class GetMyApis
{
    /// <summary>Query para listar APIs de um owner.</summary>
    public sealed record Query(Guid OwnerId, int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de APIs do owner.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.OwnerId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que retorna APIs de um owner.
    /// Lista todos os ApiAssets e filtra pelos que pertencem a serviços do owner.
    /// </summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IContractVersionRepository contractVersionRepository,
        ISubscriptionRepository subscriptionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Fetch all API assets (the repository includes OwnerService navigation)
            var allApis = await apiAssetRepository.ListAllAsync(cancellationToken);

            // Filter by owner — match on OwnerService Id
            var ownedApis = allApis
                .Where(api => api.OwnerService?.Id.Value == request.OwnerId)
                .ToList();

            // Apply pagination
            var totalCount = ownedApis.Count;
            var pagedApis = ownedApis
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Enrich with contract and subscription data
            var items = new List<OwnedApiDto>();
            foreach (var api in pagedApis)
            {
                var latestContract = await contractVersionRepository.GetLatestByApiAssetAsync(
                    api.Id.Value, cancellationToken);
                var subscribers = await subscriptionRepository.GetByApiAssetAsync(
                    api.Id.Value, cancellationToken);

                items.Add(new OwnedApiDto(
                    ApiAssetId: api.Id.Value,
                    Name: api.Name,
                    Description: null,
                    CurrentVersion: latestContract?.SemVer,
                    Status: latestContract?.LifecycleState.ToString() ?? "Unknown",
                    ConsumerCount: api.ConsumerRelationships.Count,
                    SubscriberCount: subscribers.Count,
                    LastDeployment: null));
            }

            return Result<Response>.Success(new Response(
                Items: items.AsReadOnly(),
                TotalCount: totalCount));
        }
    }

    /// <summary>DTO de API de propriedade do utilizador com métricas de consumo.</summary>
    public sealed record OwnedApiDto(
        Guid ApiAssetId,
        string Name,
        string? Description,
        string? CurrentVersion,
        string Status,
        int ConsumerCount,
        int SubscriberCount,
        DateTimeOffset? LastDeployment);

    /// <summary>Resposta com APIs de propriedade do owner.</summary>
    public sealed record Response(IReadOnlyList<OwnedApiDto> Items, int TotalCount);
}
