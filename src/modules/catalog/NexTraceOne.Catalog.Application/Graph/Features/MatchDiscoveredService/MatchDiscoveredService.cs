using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.MatchDiscoveredService;

/// <summary>
/// Feature: MatchDiscoveredService — associa um serviço descoberto a um ServiceAsset existente.
/// Transição de estado: Pending → Matched.
/// </summary>
public static class MatchDiscoveredService
{
    /// <summary>Comando de match manual.</summary>
    public sealed record Command(
        Guid DiscoveredServiceId,
        Guid ServiceAssetId) : ICommand<Response>;

    /// <summary>Valida a entrada.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DiscoveredServiceId).NotEmpty();
            RuleFor(x => x.ServiceAssetId).NotEmpty();
        }
    }

    /// <summary>Handler que faz o match.</summary>
    public sealed class Handler(
        IDiscoveredServiceRepository discoveredServiceRepository,
        IServiceAssetRepository serviceAssetRepository,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var discovered = await discoveredServiceRepository.GetByIdAsync(
                DiscoveredServiceId.From(request.DiscoveredServiceId), cancellationToken);
            if (discovered is null)
            {
                return CatalogGraphErrors.DiscoveredServiceNotFound(request.DiscoveredServiceId);
            }

            if (discovered.Status != DiscoveryStatus.Pending)
            {
                return CatalogGraphErrors.DiscoveredServiceAlreadyProcessed(request.DiscoveredServiceId, discovered.Status.ToString());
            }

            var serviceAsset = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceAssetId), cancellationToken);
            if (serviceAsset is null)
            {
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceAssetId);
            }

            discovered.MatchToService(request.ServiceAssetId);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(discovered.Id.Value, "Matched", request.ServiceAssetId, serviceAsset.Name);
        }
    }

    /// <summary>Resposta do match.</summary>
    public sealed record Response(Guid DiscoveredServiceId, string NewStatus, Guid ServiceAssetId, string ServiceAssetName);
}
