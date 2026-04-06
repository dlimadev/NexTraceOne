using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.ListFrameworkConsumers;

/// <summary>
/// Feature: ListFrameworkConsumers — lista os serviços consumidores conhecidos de um Framework/SDK.
/// A pesquisa é feita através dos perfis de dependências que referenciam o pacote do framework.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListFrameworkConsumers
{
    /// <summary>Query para listar consumidores de um framework pelo identificador do serviço.</summary>
    public sealed record Query(Guid ServiceAssetId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceAssetId).NotEmpty();
        }
    }

    /// <summary>Handler que resolve os consumidores do framework a partir dos perfis de dependências.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IFrameworkAssetDetailRepository frameworkRepository,
        IServiceDependencyProfileRepository dependencyProfileRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var serviceAssetId = ServiceAssetId.From(request.ServiceAssetId);

            var serviceAsset = await serviceAssetRepository.GetByIdAsync(serviceAssetId, cancellationToken);
            if (serviceAsset is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceAssetId);

            var frameworkDetail = await frameworkRepository.GetByServiceAssetIdAsync(serviceAssetId, cancellationToken);
            if (frameworkDetail is null)
                return CatalogGraphErrors.FrameworkDetailNotFound(request.ServiceAssetId);

            var profiles = await dependencyProfileRepository.ListByPackageNameAsync(
                frameworkDetail.PackageName, cancellationToken);

            var consumerServiceIds = profiles
                .Where(p => p.ServiceId != request.ServiceAssetId)
                .Select(p => p.ServiceId)
                .Distinct()
                .ToList();

            if (consumerServiceIds.Count == 0)
                return new Response(request.ServiceAssetId, frameworkDetail.PackageName, []);

            var allServiceAssets = await serviceAssetRepository.ListAllAsync(cancellationToken);
            var assetLookup = allServiceAssets.ToDictionary(a => a.Id.Value);

            var consumers = consumerServiceIds
                .Where(id => assetLookup.ContainsKey(id))
                .Select(id => assetLookup[id])
                .Select(a => new ConsumerItem(a.Id.Value, a.Name, a.Domain, a.TeamName))
                .ToList()
                .AsReadOnly();

            return new Response(request.ServiceAssetId, frameworkDetail.PackageName, consumers);
        }
    }

    /// <summary>Item de consumidor do framework.</summary>
    public sealed record ConsumerItem(
        Guid ServiceAssetId,
        string ServiceName,
        string Domain,
        string TeamName);

    /// <summary>Resposta da query de consumidores do framework.</summary>
    public sealed record Response(
        Guid FrameworkServiceAssetId,
        string PackageName,
        IReadOnlyList<ConsumerItem> Consumers);
}
