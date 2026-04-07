using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.RegisterServiceAsset;

/// <summary>
/// Feature: RegisterServiceAsset — registra um novo serviço no catálogo.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// Aceita os 11 campos recolhidos pelo formulário frontend, aplicando detalhes
/// e ownership opcionais imediatamente após a criação.
/// </summary>
public static class RegisterServiceAsset
{
    /// <summary>Comando de registo de um serviço no catálogo com todos os campos suportados.</summary>
    public sealed record Command(
        string Name,
        string Domain,
        string TeamName,
        string? Description = null,
        string? ServiceType = null,
        string? Criticality = null,
        string? ExposureType = null,
        string? TechnicalOwner = null,
        string? BusinessOwner = null,
        string? DocumentationUrl = null,
        string? RepositoryUrl = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de serviço.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Domain).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
            RuleFor(x => x.ServiceType).MaximumLength(100).When(x => x.ServiceType is not null);
            RuleFor(x => x.Criticality).MaximumLength(100).When(x => x.Criticality is not null);
            RuleFor(x => x.ExposureType).MaximumLength(100).When(x => x.ExposureType is not null);
            RuleFor(x => x.TechnicalOwner).MaximumLength(200).When(x => x.TechnicalOwner is not null);
            RuleFor(x => x.BusinessOwner).MaximumLength(200).When(x => x.BusinessOwner is not null);
            RuleFor(x => x.DocumentationUrl).MaximumLength(500).When(x => x.DocumentationUrl is not null);
            RuleFor(x => x.RepositoryUrl).MaximumLength(500).When(x => x.RepositoryUrl is not null);
        }
    }

    /// <summary>Handler que regista um novo serviço no catálogo.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IConfigurationResolutionService configurationService,
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

            var hasDetails = request.Description is not null
                || request.ServiceType is not null
                || request.Criticality is not null
                || request.ExposureType is not null
                || request.DocumentationUrl is not null
                || request.RepositoryUrl is not null;

            if (hasDetails)
            {
                var serviceType = ParseEnumOrDefault<Domain.Graph.Enums.ServiceType>(request.ServiceType);
                var criticality = ParseEnumOrDefault<Domain.Graph.Enums.Criticality>(request.Criticality);
                var exposureType = ParseEnumOrDefault<Domain.Graph.Enums.ExposureType>(request.ExposureType);

                serviceAsset.UpdateDetails(
                    serviceAsset.DisplayName,
                    request.Description ?? string.Empty,
                    serviceType,
                    serviceAsset.SystemArea,
                    criticality,
                    serviceAsset.LifecycleStatus,
                    exposureType,
                    request.DocumentationUrl ?? string.Empty,
                    request.RepositoryUrl ?? string.Empty);
            }

            var hasOwnership = request.TechnicalOwner is not null
                || request.BusinessOwner is not null;

            if (hasOwnership)
            {
                serviceAsset.UpdateOwnership(
                    request.TeamName,
                    request.TechnicalOwner ?? string.Empty,
                    request.BusinessOwner ?? string.Empty);
            }

            // ── PARAMETERIZATION: approval gate ──────────────────────────────
            var approvalConfig = await configurationService.ResolveEffectiveValueAsync(
                "catalog.service.creation.approval_required",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            var requiresApproval = approvalConfig?.EffectiveValue == "true";

            if (requiresApproval)
            {
                serviceAsset.UpdateLifecycleStatus(LifecycleStatus.PendingApproval);
            }

            serviceAssetRepository.Add(serviceAsset);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                serviceAsset.Id.Value,
                serviceAsset.Name,
                serviceAsset.Domain,
                serviceAsset.TeamName,
                serviceAsset.DisplayName,
                serviceAsset.Description,
                serviceAsset.ServiceType.ToString(),
                serviceAsset.Criticality.ToString(),
                serviceAsset.LifecycleStatus.ToString(),
                serviceAsset.ExposureType.ToString(),
                serviceAsset.TechnicalOwner,
                serviceAsset.BusinessOwner,
                serviceAsset.DocumentationUrl,
                serviceAsset.RepositoryUrl,
                requiresApproval);
        }

        /// <summary>Parse seguro de string para enum — retorna o valor default do enum se a conversão falhar.</summary>
        private static TEnum ParseEnumOrDefault<TEnum>(string? value) where TEnum : struct, Enum
            => Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : default;
    }

    /// <summary>Resposta do registo do serviço no catálogo.</summary>
    public sealed record Response(
        Guid ServiceAssetId,
        string Name,
        string Domain,
        string TeamName,
        string DisplayName,
        string Description,
        string ServiceType,
        string Criticality,
        string LifecycleStatus,
        string ExposureType,
        string TechnicalOwner,
        string BusinessOwner,
        string DocumentationUrl,
        string RepositoryUrl,
        bool IsPendingApproval = false);
}
