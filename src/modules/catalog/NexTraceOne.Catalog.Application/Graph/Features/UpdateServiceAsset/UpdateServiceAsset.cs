using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.UpdateServiceAsset;

/// <summary>
/// Feature: UpdateServiceAsset — atualiza detalhes e classificação de um serviço existente.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class UpdateServiceAsset
{
    /// <summary>Comando de atualização de detalhes do serviço.</summary>
    public sealed record Command(
        Guid ServiceId,
        string DisplayName,
        string Description,
        ServiceType ServiceType,
        string SystemArea,
        Criticality Criticality,
        LifecycleStatus LifecycleStatus,
        ExposureType ExposureType,
        string DocumentationUrl,
        string RepositoryUrl) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de atualização.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.DisplayName).MaximumLength(300);
            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.SystemArea).MaximumLength(200);
            RuleFor(x => x.DocumentationUrl).MaximumLength(1000);
            RuleFor(x => x.RepositoryUrl).MaximumLength(1000);
        }
    }

    /// <summary>Handler que atualiza os detalhes de um serviço existente.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);

            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            service.UpdateDetails(
                request.DisplayName,
                request.Description,
                request.ServiceType,
                request.SystemArea,
                request.Criticality,
                request.LifecycleStatus,
                request.ExposureType,
                request.DocumentationUrl,
                request.RepositoryUrl);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                service.Id.Value,
                service.Name,
                service.DisplayName,
                service.Description,
                service.ServiceType.ToString(),
                service.Domain,
                service.SystemArea,
                service.TeamName,
                service.Criticality.ToString(),
                service.LifecycleStatus.ToString(),
                service.ExposureType.ToString());
        }
    }

    /// <summary>Resposta da atualização de detalhes do serviço.</summary>
    public sealed record Response(
        Guid ServiceId,
        string Name,
        string DisplayName,
        string Description,
        string ServiceType,
        string Domain,
        string SystemArea,
        string TeamName,
        string Criticality,
        string LifecycleStatus,
        string ExposureType);
}
