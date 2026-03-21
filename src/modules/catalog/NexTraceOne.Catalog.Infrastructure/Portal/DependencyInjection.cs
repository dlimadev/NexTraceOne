using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Contracts.Portal.ServiceInterfaces;
using NexTraceOne.Catalog.Infrastructure.Portal.Persistence;
using NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Repositories;
using NexTraceOne.Catalog.Infrastructure.Portal.Services;

namespace NexTraceOne.Catalog.Infrastructure.Portal;

/// <summary>
/// Registra serviços de infraestrutura do módulo DeveloperPortal.
/// Inclui: DbContext com interceptors (auditoria + RLS), repositórios,
/// serviço de contrato público cross-module e serviços de integração.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adiciona infraestrutura do DeveloperPortal ao contentor de DI.
    /// Segue o padrão dos demais módulos: Building Blocks → Connection String → DbContext → UoW → Repos → Cross-module service.
    /// </summary>
    public static IServiceCollection AddDeveloperPortalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("DeveloperPortalDatabase", "NexTraceOne");

        services.AddDbContext<DeveloperPortalDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<AuditInterceptor>(),
                    serviceProvider.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DeveloperPortalDbContext>());
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPlaygroundSessionRepository, PlaygroundSessionRepository>();
        services.AddScoped<ICodeGenerationRepository, CodeGenerationRepository>();
        services.AddScoped<IPortalAnalyticsRepository, PortalAnalyticsRepository>();
        services.AddScoped<ISavedSearchRepository, SavedSearchRepository>();

        // Contrato público cross-module — permite que outros módulos consultem
        // dados de subscrições sem acessar o DbContext do DeveloperPortal.
        services.AddScoped<IDeveloperPortalModule, DeveloperPortalModuleService>();

        return services;
    }
}
