using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.ListServices;

/// <summary>
/// Feature: ListServices — lista serviços do catálogo com filtros opcionais.
/// Ponto de entrada principal para o catálogo de serviços do NexTraceOne.
/// Estrutura VSA: Query + Handler + Response em um único arquivo.
/// </summary>
public static class ListServices
{
    /// <summary>Query de listagem filtrada de serviços do catálogo.</summary>
    public sealed record Query(
        string? TeamName,
        string? Domain,
        ServiceType? ServiceType,
        Criticality? Criticality,
        LifecycleStatus? LifecycleStatus,
        ExposureType? ExposureType,
        string? SearchTerm) : IQuery<Response>;

    /// <summary>Handler que lista serviços com filtros opcionais.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var services = await serviceAssetRepository.ListFilteredAsync(
                request.TeamName,
                request.Domain,
                request.ServiceType,
                request.Criticality,
                request.LifecycleStatus,
                request.ExposureType,
                request.SearchTerm,
                cancellationToken);

            var items = services
                .Select(svc => new ServiceListItem(
                    svc.Id.Value,
                    svc.Name,
                    svc.DisplayName,
                    svc.Description,
                    svc.ServiceType.ToString(),
                    svc.Domain,
                    svc.SystemArea,
                    svc.TeamName,
                    svc.TechnicalOwner,
                    svc.Criticality.ToString(),
                    svc.LifecycleStatus.ToString(),
                    svc.ExposureType.ToString()))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de serviços do catálogo.</summary>
    public sealed record Response(
        IReadOnlyList<ServiceListItem> Items,
        int TotalCount);

    /// <summary>Item resumido de um serviço na listagem do catálogo.</summary>
    public sealed record ServiceListItem(
        Guid ServiceId,
        string Name,
        string DisplayName,
        string Description,
        string ServiceType,
        string Domain,
        string SystemArea,
        string TeamName,
        string TechnicalOwner,
        string Criticality,
        string LifecycleStatus,
        string ExposureType);
}
