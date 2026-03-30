using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListContracts;

/// <summary>
/// Feature: ListContracts — lista contratos do catálogo com filtros opcionais.
/// Retorna a versão mais recente de cada contrato (por ApiAssetId distinto),
/// oferecendo a visão principal de governança de contratos do NexTraceOne.
/// Inclui dados enriquecidos do ServiceAsset associado (domain, team, owner, criticality).
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListContracts
{
    /// <summary>Query de listagem do catálogo de contratos com filtros e paginação.</summary>
    public sealed record Query(
        ContractProtocol? Protocol,
        ContractLifecycleState? LifecycleState,
        string? SearchTerm,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de listagem.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista contratos mais recentes por API asset com dados enriquecidos do serviço.</summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IApiAssetRepository apiAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Busca as versões mais recentes de contrato
            var (items, totalCount) = await repository.ListLatestPerApiAssetAsync(
                request.Protocol,
                request.LifecycleState,
                request.SearchTerm,
                request.Page,
                request.PageSize,
                cancellationToken);

            // Batch lookup dos ApiAssets com ServiceAsset incluído
            var apiAssetIds = items.Select(v => v.ApiAssetId).Distinct();
            var apiAssetsDict = await apiAssetRepository.ListByApiAssetIdsAsync(apiAssetIds, cancellationToken);

            // Enriquece cada contrato com dados do serviço
            var contracts = items.Select(v =>
            {
                apiAssetsDict.TryGetValue(v.ApiAssetId, out var apiAsset);
                var service = apiAsset?.OwnerService;

                return new ContractListItem(
                    VersionId: v.Id.Value,
                    ApiAssetId: v.ApiAssetId,
                    ServiceAssetId: service?.Id.Value,
                    Name: apiAsset?.Name ?? "Unknown",
                    SemVer: v.SemVer,
                    Protocol: v.Protocol.ToString(),
                    LifecycleState: v.LifecycleState.ToString(),
                    IsLocked: v.IsLocked,
                    Format: v.Format,
                    ImportedFrom: v.ImportedFrom,
                    CreatedAt: v.CreatedAt,
                    UpdatedAt: v.UpdatedAt != default ? v.UpdatedAt : v.CreatedAt,
                    DeprecationDate: v.DeprecationDate,
                    IsSigned: v.Signature is not null,
                    Domain: service?.Domain ?? string.Empty,
                    Team: service?.TeamName ?? string.Empty,
                    TechnicalOwner: service?.TechnicalOwner ?? string.Empty,
                    Criticality: service?.Criticality.ToString() ?? "Medium",
                    Exposure: service?.ExposureType.ToString() ?? "Internal",
                    ServiceType: service?.ServiceType.ToString() ?? "RestApi",
                    OverallScore: v.LastOverallScore);
            }).ToList();

            return new Response(contracts, totalCount, request.Page, request.PageSize);
        }
    }

    /// <summary>
    /// Item de contrato na listagem do catálogo de governança.
    /// Inclui dados enriquecidos do ServiceAsset associado via ApiAsset.
    /// </summary>
    public sealed record ContractListItem(
        Guid VersionId,
        Guid ApiAssetId,
        Guid? ServiceAssetId,
        string Name,
        string SemVer,
        string Protocol,
        string LifecycleState,
        bool IsLocked,
        string Format,
        string ImportedFrom,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DateTimeOffset? DeprecationDate,
        bool IsSigned,
        string Domain,
        string Team,
        string TechnicalOwner,
        string Criticality,
        string Exposure,
        string ServiceType,
        decimal? OverallScore);

    /// <summary>Resposta paginada da listagem de contratos.</summary>
    public sealed record Response(
        IReadOnlyList<ContractListItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
