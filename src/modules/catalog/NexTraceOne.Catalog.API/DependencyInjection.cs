using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.Catalog.Application.Contracts;
using NexTraceOne.Catalog.Application.DependencyGovernance;
using NexTraceOne.Catalog.Application.DeveloperExperience;
using NexTraceOne.Catalog.Application.Graph;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.GetLegacyAssetDetail;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.GetLegacyImpactPropagation;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.ListLegacyAssets;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCicsTransaction;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCobolProgram;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCopybook;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterDb2Artifact;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterImsTransaction;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterMainframeSystem;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterZosConnectBinding;
using NexTraceOne.Catalog.Application.LegacyAssets.Features.SyncLegacyAssets;
using NexTraceOne.Catalog.Application.Portal;
using NexTraceOne.Catalog.Application.Services;
using NexTraceOne.Catalog.Application.SourceOfTruth;
using NexTraceOne.Catalog.Application.Templates;
using NexTraceOne.Catalog.Infrastructure;

namespace NexTraceOne.Catalog.API;

/// <summary>
/// Registra todos os serviços do módulo ServiceCatalog (Application + Infrastructure).
/// Substitui as chamadas individuais por sub-módulo (AddCatalogGraphModule, AddContractsModule, etc.)
/// num único ponto de entrada consolidado.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adiciona todos os serviços do módulo ServiceCatalog ao container DI.</summary>
    public static IServiceCollection AddServiceCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Application layers ────────────────────────────────────────────
        services.AddCatalogGraphApplication(configuration);
        services.AddCatalogServicesApplication(configuration);
        services.AddSourceOfTruthApplication(configuration);
        services.AddContractsApplication(configuration);
        services.AddCatalogDependencyGovernanceApplication(configuration);
        services.AddDeveloperExperienceApplication(configuration);
        services.AddDeveloperPortalApplication(configuration);
        services.AddCatalogTemplatesApplication(configuration);

        // ── LegacyAssets validators (no Application DI — uses MediatR scanning) ──
        services.AddTransient<IValidator<RegisterMainframeSystem.Command>, RegisterMainframeSystem.Validator>();
        services.AddTransient<IValidator<RegisterCobolProgram.Command>, RegisterCobolProgram.Validator>();
        services.AddTransient<IValidator<RegisterCopybook.Command>, RegisterCopybook.Validator>();
        services.AddTransient<IValidator<RegisterCicsTransaction.Command>, RegisterCicsTransaction.Validator>();
        services.AddTransient<IValidator<RegisterImsTransaction.Command>, RegisterImsTransaction.Validator>();
        services.AddTransient<IValidator<RegisterDb2Artifact.Command>, RegisterDb2Artifact.Validator>();
        services.AddTransient<IValidator<RegisterZosConnectBinding.Command>, RegisterZosConnectBinding.Validator>();
        services.AddTransient<IValidator<ListLegacyAssets.Query>, ListLegacyAssets.Validator>();
        services.AddTransient<IValidator<GetLegacyAssetDetail.Query>, GetLegacyAssetDetail.Validator>();
        services.AddTransient<IValidator<SyncLegacyAssets.Command>, SyncLegacyAssets.Validator>();
        services.AddTransient<IValidator<GetLegacyImpactPropagation.Query>, GetLegacyImpactPropagation.Validator>();

        // ── Consolidated Infrastructure (single DbContext, all repos) ─────
        services.AddServiceCatalogInfrastructure(configuration);

        return services;
    }
}
