using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.CreateServiceInterface;

/// <summary>
/// Feature: CreateServiceInterface — regista uma nova interface exposta por um serviço.
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class CreateServiceInterface
{
    /// <summary>Comando de criação de uma interface de serviço.</summary>
    public sealed record Command(
        Guid ServiceAssetId,
        string Name,
        string InterfaceType,
        string? Description = null,
        string? ExposureScope = null,
        string? BasePath = null,
        string? TopicName = null,
        string? WsdlNamespace = null,
        string? GrpcServiceName = null,
        string? ScheduleCron = null,
        string? DocumentationUrl = null,
        bool? RequiresContract = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de interface.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceAssetId).NotEqual(Guid.Empty).WithMessage("ServiceAssetId is required.");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
            RuleFor(x => x.InterfaceType).NotEmpty();
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
            RuleFor(x => x.BasePath).MaximumLength(500).When(x => x.BasePath is not null);
            RuleFor(x => x.TopicName).MaximumLength(500).When(x => x.TopicName is not null);
            RuleFor(x => x.WsdlNamespace).MaximumLength(500).When(x => x.WsdlNamespace is not null);
            RuleFor(x => x.GrpcServiceName).MaximumLength(300).When(x => x.GrpcServiceName is not null);
            RuleFor(x => x.DocumentationUrl).MaximumLength(1000).When(x => x.DocumentationUrl is not null);
        }
    }

    /// <summary>Handler que cria uma nova interface de serviço.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IServiceInterfaceRepository serviceInterfaceRepository,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceAssetId),
                cancellationToken);

            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceAssetId);

            if (!Enum.TryParse<InterfaceType>(request.InterfaceType, ignoreCase: true, out var interfaceType))
                return Error.Validation("CatalogGraph.ServiceInterface.InvalidType",
                    "Interface type '{0}' is not valid.", request.InterfaceType);

            var iface = ServiceInterface.Create(request.ServiceAssetId, request.Name, interfaceType);

            var exposureScope = ParseEnumOrDefault<ExposureType>(request.ExposureScope);
            iface.UpdateDetails(
                description: request.Description ?? string.Empty,
                exposureScope: exposureScope,
                basePath: request.BasePath ?? string.Empty,
                topicName: request.TopicName ?? string.Empty,
                wsdlNamespace: request.WsdlNamespace ?? string.Empty,
                grpcServiceName: request.GrpcServiceName ?? string.Empty,
                scheduleCron: request.ScheduleCron ?? string.Empty,
                environmentId: string.Empty,
                sloTarget: string.Empty,
                requiresContract: request.RequiresContract ?? false,
                authScheme: InterfaceAuthScheme.None,
                rateLimitPolicy: string.Empty,
                documentationUrl: request.DocumentationUrl ?? string.Empty);

            serviceInterfaceRepository.Add(iface);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                iface.Id.Value,
                iface.ServiceAssetId,
                iface.Name,
                iface.InterfaceType.ToString(),
                iface.Status.ToString());
        }

        private static TEnum ParseEnumOrDefault<TEnum>(string? value) where TEnum : struct, Enum
            => Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : default;
    }

    /// <summary>Resposta da criação da interface de serviço.</summary>
    public sealed record Response(
        Guid InterfaceId,
        Guid ServiceAssetId,
        string Name,
        string InterfaceType,
        string Status);
}
