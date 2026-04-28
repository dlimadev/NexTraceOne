using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.Catalog.Application.Services.Features.GetOnboardingHealthReport;
using NexTraceOne.Catalog.Application.Services.Features.GetServiceMigrationProgressReport;
using NexTraceOne.Catalog.Application.Services.Features.GetServiceRetirementReadinessReport;

namespace NexTraceOne.Catalog.Application.Services;

/// <summary>
/// Registra serviços da camada Application do módulo Catalog Services.
/// Inclui: MediatR handlers, FluentValidation validators para todas as features
/// de relatórios de serviços.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogServicesApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(configuration);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // ── Wave AC.1 — Onboarding Health Report ──────────────────────────
        services.AddTransient<IValidator<GetOnboardingHealthReport.Query>, GetOnboardingHealthReport.Validator>();

        // ── Wave AF.2 — Service Retirement Readiness Report ────────────────
        services.AddTransient<IValidator<GetServiceRetirementReadinessReport.Query>, GetServiceRetirementReadinessReport.Validator>();

        // ── Wave AF.3 — Service Migration Progress Report ────────────────
        services.AddTransient<IValidator<GetServiceMigrationProgressReport.Query>, GetServiceMigrationProgressReport.Validator>();

        return services;
    }
}
