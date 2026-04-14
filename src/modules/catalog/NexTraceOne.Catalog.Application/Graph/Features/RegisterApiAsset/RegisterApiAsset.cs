using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.RegisterApiAsset;

/// <summary>
/// Feature: RegisterApiAsset — registra uma nova API no grafo de engenharia.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterApiAsset
{
    /// <summary>Comando de registo de um ativo de API.</summary>
    public sealed record Command(
        string Name,
        string RoutePattern,
        string Version,
        string Visibility,
        Guid OwnerServiceAssetId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de API.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RoutePattern).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Visibility).NotEmpty().MaximumLength(50);
            RuleFor(x => x.OwnerServiceAssetId).NotEmpty();
        }
    }

    /// <summary>Handler que regista um novo ativo de API no grafo.</summary>
    public sealed class Handler(
        IApiAssetRepository apiAssetRepository,
        IServiceAssetRepository serviceAssetRepository,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var ownerServiceId = ServiceAssetId.From(request.OwnerServiceAssetId);
            var ownerService = await serviceAssetRepository.GetByIdAsync(ownerServiceId, cancellationToken);
            if (ownerService is null)
            {
                return CatalogGraphErrors.ServiceAssetNotFound(request.OwnerServiceAssetId.ToString());
            }

            var existing = await apiAssetRepository.GetByNameAndOwnerAsync(request.Name, ownerServiceId, cancellationToken);
            if (existing is not null)
            {
                return CatalogGraphErrors.ApiAssetAlreadyExists(request.Name);
            }

            var apiAsset = ApiAsset.Register(request.Name, request.RoutePattern, request.Version, request.Visibility, ownerService);
            apiAssetRepository.Add(apiAsset);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                apiAsset.Id.Value,
                apiAsset.Name,
                apiAsset.RoutePattern,
                apiAsset.Version,
                apiAsset.Visibility,
                ownerService.Id.Value,
                ownerService.Name);
        }
    }

    /// <summary>Resposta do registo do ativo de API.</summary>
    public sealed record Response(
        Guid ApiAssetId,
        string Name,
        string RoutePattern,
        string Version,
        string Visibility,
        Guid OwnerServiceAssetId,
        string OwnerServiceName);
}
