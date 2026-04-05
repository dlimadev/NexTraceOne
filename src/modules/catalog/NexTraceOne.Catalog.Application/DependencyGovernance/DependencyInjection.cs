using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.CheckDependencyPolicies;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.CompareDependencyVersions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.DetectLicenseConflicts;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GenerateSbom;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetDependencyHealthDashboard;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetServiceDependencyProfile;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.GetTemplateDependencyHealth;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.ListVulnerableDependencies;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.ScanServiceDependencies;
using NexTraceOne.Catalog.Application.DependencyGovernance.Features.SuggestDependencyUpgrades;

namespace NexTraceOne.Catalog.Application.DependencyGovernance;

/// <summary>
/// Registra serviços da camada de aplicação do módulo Dependency Governance.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogDependencyGovernanceApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddTransient<IValidator<ScanServiceDependencies.Command>, ScanServiceDependencies.Validator>();
        services.AddTransient<IValidator<GetServiceDependencyProfile.Query>, GetServiceDependencyProfile.Validator>();
        services.AddTransient<IValidator<ListVulnerableDependencies.Query>, ListVulnerableDependencies.Validator>();
        services.AddTransient<IValidator<GetDependencyHealthDashboard.Query>, GetDependencyHealthDashboard.Validator>();
        services.AddTransient<IValidator<CheckDependencyPolicies.Query>, CheckDependencyPolicies.Validator>();
        services.AddTransient<IValidator<GenerateSbom.Command>, GenerateSbom.Validator>();
        services.AddTransient<IValidator<CompareDependencyVersions.Query>, CompareDependencyVersions.Validator>();
        services.AddTransient<IValidator<SuggestDependencyUpgrades.Query>, SuggestDependencyUpgrades.Validator>();
        services.AddTransient<IValidator<DetectLicenseConflicts.Query>, DetectLicenseConflicts.Validator>();
        services.AddTransient<IValidator<GetTemplateDependencyHealth.Query>, GetTemplateDependencyHealth.Validator>();

        return services;
    }
}
