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

            var consumers = new List<ConsumerItem>();
            foreach (var profile in profiles)
            {
                if (profile.ServiceId == request.ServiceAssetId)
                    continue;

                var consumerAsset = await serviceAssetRepository.GetByIdAsync(
                    ServiceAssetId.From(profile.ServiceId), cancellationToken);

                if (consumerAsset is null)
                    continue;

                consumers.Add(new ConsumerItem(
                    consumerAsset.Id.Value,
                    consumerAsset.Name,
                    consumerAsset.Domain,
                    consumerAsset.TeamName));
            }

            return new Response(
                request.ServiceAssetId,
                frameworkDetail.PackageName,
                consumers.AsReadOnly());
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
