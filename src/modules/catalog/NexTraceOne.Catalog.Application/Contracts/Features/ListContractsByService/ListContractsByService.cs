using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Contracts.Application.Features.ListContractsByService;

/// <summary>
/// Feature: ListContractsByService — lista contratos associados a um serviço do catálogo.
/// Resolve a relação Service → APIs → ContractVersions.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListContractsByService
{
    /// <summary>Query de contratos associados a um serviço.</summary>
    public sealed record Query(Guid ServiceId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    /// <summary>Handler que lista contratos vinculados a um serviço via suas APIs.</summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IContractVersionRepository contractRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Obter APIs do serviço
            var apis = await apiAssetRepository.ListByServiceIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);

            if (apis.Count == 0)
                return new Response(request.ServiceId, [], 0);

            // Obter contratos mais recentes para cada API
            var apiAssetIds = apis.Select(a => a.Id.Value).ToList();
            var contracts = await contractRepository.ListByApiAssetIdsAsync(apiAssetIds, cancellationToken);

            var items = contracts
                .Select(v =>
                {
                    var api = apis.FirstOrDefault(a => a.Id.Value == v.ApiAssetId);
                    return new ServiceContractItem(
                        v.Id.Value,
                        v.ApiAssetId,
                        api?.Name ?? string.Empty,
                        api?.RoutePattern ?? string.Empty,
                        v.SemVer,
                        v.Protocol.ToString(),
                        v.LifecycleState.ToString(),
                        v.IsLocked,
                        v.CreatedAt);
                })
                .ToList();

            return new Response(request.ServiceId, items, items.Count);
        }
    }

    /// <summary>Contrato associado a um serviço via API asset.</summary>
    public sealed record ServiceContractItem(
        Guid VersionId,
        Guid ApiAssetId,
        string ApiName,
        string ApiRoutePattern,
        string SemVer,
        string Protocol,
        string LifecycleState,
        bool IsLocked,
        DateTimeOffset CreatedAt);

    /// <summary>Resposta da listagem de contratos de um serviço.</summary>
    public sealed record Response(
        Guid ServiceId,
        IReadOnlyList<ServiceContractItem> Contracts,
        int TotalCount);
}
