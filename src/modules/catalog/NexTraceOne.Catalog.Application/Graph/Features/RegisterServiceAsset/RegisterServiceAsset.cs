using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.RegisterServiceAsset;

/// <summary>
/// Feature: RegisterServiceAsset — registra um novo serviço no catálogo.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterServiceAsset
{
    /// <summary>Comando de registo de um serviço no catálogo.</summary>
    public sealed record Command(string Name, string Domain, string TeamName) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de serviço.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Domain).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que regista um novo serviço no catálogo.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var existing = await serviceAssetRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existing is not null)
            {
                return CatalogGraphErrors.ServiceAssetAlreadyExists(request.Name);
            }

            var serviceAsset = ServiceAsset.Create(request.Name, request.Domain, request.TeamName);
            serviceAssetRepository.Add(serviceAsset);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                serviceAsset.Id.Value,
                serviceAsset.Name,
                serviceAsset.Domain,
                serviceAsset.TeamName,
                serviceAsset.DisplayName,
                serviceAsset.ServiceType.ToString(),
                serviceAsset.Criticality.ToString(),
                serviceAsset.LifecycleStatus.ToString());
        }
    }

    /// <summary>Resposta do registo do serviço no catálogo.</summary>
    public sealed record Response(
        Guid ServiceAssetId,
        string Name,
        string Domain,
        string TeamName,
        string DisplayName,
        string ServiceType,
        string Criticality,
        string LifecycleStatus);
}
