using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Portal.Abstractions;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetApisIConsume;

/// <summary>
/// Feature: GetApisIConsume — painel do consumidor com APIs que o utilizador/serviço consome.
/// Consulta subscrições do Portal e enriquece com dados de contratos e alertas.
/// </summary>
public static class GetApisIConsume
{
    /// <summary>Query para obter APIs consumidas por um utilizador ou serviço.</summary>
    public sealed record Query(Guid UserId, int Page = 1, int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros da consulta de APIs consumidas.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que retorna APIs consumidas com status e alertas.
    /// Consulta subscrições do utilizador, enriquece com dados de contrato e owner.
    /// </summary>
    public sealed class Handler(
        ISubscriptionRepository subscriptionRepository,
        IContractVersionRepository contractVersionRepository,
        IApiAssetRepository apiAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Fetch all subscriptions for this user
            var subscriptions = await subscriptionRepository.GetBySubscriberAsync(
                request.UserId, cancellationToken);

            // Apply pagination
            var totalCount = subscriptions.Count;
            var pagedSubs = subscriptions
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Batch-lookup ApiAssets for enrichment
            var apiAssetIds = pagedSubs.Select(s => s.ApiAssetId).Distinct().ToList();
            var apiAssets = apiAssetIds.Count > 0
                ? await apiAssetRepository.ListByApiAssetIdsAsync(apiAssetIds, cancellationToken)
                : new Dictionary<Guid, Domain.Graph.Entities.ApiAsset>();

            var breakingChanges = 0;
            var deprecations = 0;
            var items = new List<ConsumedApiDto>();

            foreach (var sub in pagedSubs)
            {
                var latestContract = await contractVersionRepository.GetLatestByApiAssetAsync(
                    sub.ApiAssetId, cancellationToken);

                apiAssets.TryGetValue(sub.ApiAssetId, out var apiAsset);

                var isDeprecated = latestContract?.LifecycleState is
                    Domain.Contracts.Enums.ContractLifecycleState.Deprecated or
                    Domain.Contracts.Enums.ContractLifecycleState.Sunset or
                    Domain.Contracts.Enums.ContractLifecycleState.Retired;

                if (isDeprecated) deprecations++;

                // Compute breaking changes from semantic diff data
                var hasBreakingChanges = latestContract?.Diffs.Any(d => d.BreakingChanges.Count > 0) ?? false;
                if (hasBreakingChanges) breakingChanges++;

                items.Add(new ConsumedApiDto(
                    ApiAssetId: sub.ApiAssetId,
                    ApiName: apiAsset?.Name ?? "Unknown",
                    CurrentVersion: latestContract?.SemVer,
                    LatestVersion: latestContract?.SemVer,
                    Status: latestContract?.LifecycleState.ToString() ?? "Unknown",
                    HasBreakingChanges: hasBreakingChanges,
                    IsDeprecated: isDeprecated,
                    DeprecationDate: latestContract?.DeprecationDate,
                    LastChange: latestContract?.UpdatedAt,
                    Owner: apiAsset?.OwnerService?.TechnicalOwner,
                    RiskScore: 0m));
            }

            return Result<Response>.Success(new Response(
                Items: items.AsReadOnly(),
                TotalCount: totalCount,
                PendingActions: breakingChanges + deprecations,
                BreakingChangesCount: breakingChanges,
                DeprecationsCount: deprecations));
        }
    }

    /// <summary>DTO de API consumida com status e alertas para o painel do consumidor.</summary>
    public sealed record ConsumedApiDto(
        Guid ApiAssetId,
        string ApiName,
        string? CurrentVersion,
        string? LatestVersion,
        string Status,
        bool HasBreakingChanges,
        bool IsDeprecated,
        DateTimeOffset? DeprecationDate,
        DateTimeOffset? LastChange,
        string? Owner,
        decimal RiskScore);

    /// <summary>Resposta do painel do consumidor com APIs consumidas e métricas de alerta.</summary>
    public sealed record Response(
        IReadOnlyList<ConsumedApiDto> Items,
        int TotalCount,
        int PendingActions,
        int BreakingChangesCount,
        int DeprecationsCount);
}
