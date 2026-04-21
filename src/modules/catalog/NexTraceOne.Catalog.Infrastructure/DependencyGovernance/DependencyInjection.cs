using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Configuration;
using NexTraceOne.BuildingBlocks.Infrastructure.Interceptors;
using NexTraceOne.Catalog.Application.DependencyGovernance.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence;

namespace NexTraceOne.Catalog.Infrastructure.DependencyGovernance;

/// <summary>
/// Registra serviços de infraestrutura do módulo Dependency Governance.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCatalogDependencyGovernanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBuildingBlocksInfrastructure(configuration);

        var connectionString = configuration.GetRequiredConnectionString("CatalogDatabase", "NexTraceOne");

        services.AddDbContext<DependencyGovernanceDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantRlsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DependencyGovernanceDbContext>());
        services.AddScoped<IDependencyGovernanceUnitOfWork>(sp => sp.GetRequiredService<DependencyGovernanceDbContext>());
        services.AddScoped<IServiceDependencyProfileRepository, ServiceDependencyProfileRepository>();
        services.AddScoped<IVulnerabilityAdvisoryRepository, EfVulnerabilityAdvisoryRepository>();

        return services;
    }
}
