using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.RegisterFromDiscovery;

/// <summary>
/// Feature: RegisterFromDiscovery — cria um novo ServiceAsset no catálogo
/// a partir de um serviço descoberto automaticamente.
/// Transição de estado: Pending → Registered.
/// </summary>
public static class RegisterFromDiscovery
{
    /// <summary>Comando para registar serviço a partir de discovery.</summary>
    public sealed record Command(
        Guid DiscoveredServiceId,
        string Domain,
        string TeamName) : ICommand<Response>;

    /// <summary>Valida a entrada.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DiscoveredServiceId).NotEmpty();
            RuleFor(x => x.Domain).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que regista o serviço no catálogo.</summary>
    public sealed class Handler(
        IDiscoveredServiceRepository discoveredServiceRepository,
        IServiceAssetRepository serviceAssetRepository,
        IDateTimeProvider dateTimeProvider,
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

            // Verifica se já existe serviço com o mesmo nome
            var existingService = await serviceAssetRepository.GetByNameAsync(discovered.ServiceName, cancellationToken);
            if (existingService is not null)
            {
                return CatalogGraphErrors.ServiceAssetAlreadyExists(discovered.ServiceName);
            }

            var serviceAsset = ServiceAsset.Create(discovered.ServiceName, request.Domain, request.TeamName);
            serviceAssetRepository.Add(serviceAsset);

            discovered.MarkAsRegistered(serviceAsset.Id.Value);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                discovered.Id.Value,
                serviceAsset.Id.Value,
                serviceAsset.Name,
                "Registered");
        }
    }

    /// <summary>Resposta do registo a partir de discovery.</summary>
    public sealed record Response(Guid DiscoveredServiceId, Guid ServiceAssetId, string ServiceName, string NewStatus);
}
